using System.Text.RegularExpressions;
using BfShaderSharp.Bnsh;
using BfShaderSharp.Prepared;
using OwlRenderingSharp.Blocks;
using OwlRenderingSharp.Content;
using OwlRenderingSharp.Graph;

namespace OwlRenderingSharp.Snapshot;

/// <summary>Lists, for every frame-level uniform block, the vec4 slots the programs of a model read, and whether the profile fills each one.</summary>
/// <remarks>
/// Programs are every pass program of every material of the model. A slot is read when the GLSL indexes it with a constant; indexing with a computed value is counted per block and not
/// attributed to a slot. A slot is filled when the profile writes any non-zero byte in it, and named when its layout has a field starting in it.
/// </remarks>
public sealed partial class SlotAudit(ContentSet content, IBlockProfile profile)
{
    [GeneratedRegex(@"(?<stage>fp|vp)_c(?<n>\d+)\.data\[(?<slot>\d+)\](?:\.(?<c>[xyzw]+))?")]
    private static partial Regex ConstantIndex();

    [GeneratedRegex(@"(?<stage>fp|vp)_c(?<n>\d+)\.data\[(?!\d+\])")]
    private static partial Regex ComputedIndex();

    public void Run(ModelResources model, FrameInfo frame, TextWriter output)
    {
        Dictionary<(string Block, int Slot), SortedSet<int>> reads = [];
        Dictionary<(string Block, int Slot), HashSet<char>> components = [];
        Dictionary<string, int> computed = [];
        HashSet<int> seen = [];
        foreach (MaterialResources material in model.Materials)
            foreach (RenderPass pass in Enum.GetValues<RenderPass>())
                foreach (int skin in model.Shapes.Where(s => model.Materials[s.MaterialIndex] == material).Select(s => s.Record.SkinCount).Distinct())
                {
                    if (material.Pass(pass, skin)?.Program is not { } entry || !seen.Add(entry.Index * 16 + (int)pass)) continue;
                    string[] layout = entry.LayoutId.Split('/');
                    PreparedModel prepared = content.Shaders(layout[0]).Model(layout[1])!;
                    foreach (ShaderStage stage in new[] { ShaderStage.Vertex, ShaderStage.Fragment })
                        Scan(prepared.Glsl(entry.Index, stage), entry, reads, components, computed);
                }

        foreach (string block in reads.Keys.Select(k => k.Block).Distinct().Order()) Report(block, reads, components, computed, frame, output);
    }

    private static void Scan(string? glsl, ProgramEntry entry, Dictionary<(string, int), SortedSet<int>> reads, Dictionary<(string, int), HashSet<char>> components, Dictionary<string, int> computed)
    {
        if (glsl is null) return;
        string Gsys(string stage, string n) => entry.Bindings.Blocks.FirstOrDefault(b => b.Vertex == (stage == "vp") && b.GlslName.EndsWith($"{stage}_c{n}"))?.GsysName ?? $"{stage}_c{n}";
        foreach (Match m in ConstantIndex().Matches(glsl))
        {
            string block = Gsys(m.Groups["stage"].Value, m.Groups["n"].Value);
            (string, int) key = (block, int.Parse(m.Groups["slot"].Value));
            if (!reads.TryGetValue(key, out SortedSet<int>? programs)) reads[key] = programs = [];
            programs.Add(entry.Index);
            if (!components.TryGetValue(key, out HashSet<char>? set)) components[key] = set = [];
            foreach (char c in m.Groups["c"].Value) set.Add(c);
        }

        foreach (Match m in ComputedIndex().Matches(glsl))
        {
            string block = Gsys(m.Groups["stage"].Value, m.Groups["n"].Value);
            computed[block] = computed.GetValueOrDefault(block) + 1;
        }
    }

    private void Report(string block, Dictionary<(string Block, int Slot), SortedSet<int>> reads, Dictionary<(string Block, int Slot), HashSet<char>> components, Dictionary<string, int> computed, FrameInfo frame, TextWriter output)
    {
        BlockLayout? layout = profile.Layout(block, null);
        byte[]? bytes = null;
        if (layout is not null && profile.Scope(block) == BlockScope.Frame)
        {
            BlockWriter writer = new(layout);
            profile.Fill(block, writer, frame, null);
            bytes = writer.Bytes;
        }

        int[] slots = [.. reads.Keys.Where(k => k.Block == block).Select(k => k.Slot).Order()];
        output.WriteLine($"== {block}: {slots.Length} slots read, layout {(layout is null ? "none" : $"{layout.Size} bytes, {layout.Fields.Count} named")}, computed-index reads {computed.GetValueOrDefault(block)}");
        foreach (int slot in slots)
        {
            bool named = layout?.Fields.Any(f => f.Offset / 16 == slot) ?? false;
            string filled = bytes is null ? "-" : slot * 16 + 16 <= bytes.Length && bytes.AsSpan(slot * 16, 16).ContainsAnyExcept((byte)0) ? "filled" : "ZERO";
            string name = layout?.Fields.FirstOrDefault(f => f.Offset / 16 == slot)?.Name ?? "";
            output.WriteLine($"  {slot,4} {filled,-6} {(named ? "named" : "     "),-5} {name,-22} comps {string.Concat(components[(block, slot)].Order())} programs {reads[(block, slot)].Count}");
        }
    }
}
