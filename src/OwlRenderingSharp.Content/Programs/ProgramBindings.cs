using System.Text.RegularExpressions;
using BfShaderSharp.Errors;
using BfShaderSharp.Prepared;

namespace OwlRenderingSharp.Content.Programs;

/// <summary>What a decompiled program binds, in the engine's terms: its blocks by <c>gsys_*</c> name and its samplers by the shader's sampler name.</summary>
/// <remarks>
/// A decompiled constant buffer <c>_vp_cN</c> or <c>_fp_cN</c> holds the program's block at binding slot N - 3 for that stage, and a sampler
/// <c>fp_t_tcb_H</c> the sampler at slot (H - 8) / 2, H being hexadecimal. The slots are the program's own, read from the prepared archive. Storage
/// blocks and the support buffer are listed apart since the renderer provides them; constant buffers 0-2 belong to the driver, not the program, and are left out. A declaration the archive has no name for throws
/// <see cref="BfShaderFormatException"/>.
/// </remarks>
public sealed partial class ProgramBindings
{
    public const int FirstUserConstantBuffer = 3;
    private const int FirstSamplerHandle = 8;

    public required IReadOnlyList<BlockBinding> Blocks { get; init; }

    public required IReadOnlyList<SamplerBinding> Samplers { get; init; }

    public required IReadOnlyList<string> StorageBlocks { get; init; }

    public required bool UsesSupportBuffer { get; init; }

    public static ProgramBindings For(PreparedModel model, int program, string? vertexGlsl, string? fragmentGlsl)
    {
        PreparedProgram bindings = model.Program(program);
        Dictionary<(bool, int), string> blocks = [];
        foreach (PreparedBlock block in model.Blocks())
        {
            PreparedBinding slot = bindings.UniformBlock(block.Index);
            if (slot.Vertex >= 0) blocks[(true, slot.Vertex)] = block.Name;
            if (slot.Fragment >= 0) blocks[(false, slot.Fragment)] = block.Name;
        }

        Dictionary<(bool, int), string> samplers = [];
        foreach (PreparedSampler sampler in model.Samplers())
        {
            PreparedBinding slot = bindings.Sampler(sampler.Index);
            if (slot.Vertex >= 0) samplers[(true, slot.Vertex)] = sampler.Name;
            if (slot.Fragment >= 0) samplers[(false, slot.Fragment)] = sampler.Name;
        }

        return Resolve(GlslInterface.Parse(vertexGlsl), GlslInterface.Parse(fragmentGlsl), blocks, samplers);
    }

    public static ProgramBindings Resolve(GlslInterface vertex, GlslInterface fragment, IReadOnlyDictionary<(bool Vertex, int Slot), string> blockNames, IReadOnlyDictionary<(bool Vertex, int Slot), string> samplerNames)
    {
        List<BlockBinding> blocks = [];
        List<SamplerBinding> samplers = [];
        List<string> storage = [];
        bool support = false;
        foreach ((GlslInterface stage, bool isVertex) in new[] { (vertex, true), (fragment, false) })
        {
            foreach (GlslBlock block in stage.Blocks)
            {
                if (block.IsStorage) storage.Add(block.Name);
                else if (block.Name == "_support_buffer") support = true;
                else if (SlotOfBlock(block.Name) >= 0) blocks.Add(new BlockBinding(block.Name, isVertex, Lookup(blockNames, isVertex, SlotOfBlock(block.Name), block.Name)));
            }

            foreach (GlslSampler sampler in stage.Samplers)
                samplers.Add(new SamplerBinding(sampler.Name, isVertex, sampler.Binding, sampler.Type, Lookup(samplerNames, isVertex, SlotOfSampler(sampler.Name), sampler.Name)));
        }

        return new ProgramBindings { Blocks = blocks, Samplers = samplers, StorageBlocks = storage, UsesSupportBuffer = support };
    }

    private static string Lookup(IReadOnlyDictionary<(bool, int), string> names, bool vertex, int slot, string glslName) =>
        names.TryGetValue((vertex, slot), out string? name) ? name : throw new BfShaderFormatException($"{glslName} is at slot {slot}, which the program's archive names nothing at");

    private static int SlotOfBlock(string glslName) =>
        BlockName().Match(glslName) is { Success: true } m ? int.Parse(m.Groups[1].Value) - FirstUserConstantBuffer : throw new BfShaderFormatException($"{glslName} is not a constant buffer name");

    private static int SlotOfSampler(string glslName) =>
        SamplerName().Match(glslName) is { Success: true } m ? (Convert.ToInt32(m.Groups[1].Value, 16) - FirstSamplerHandle) / 2 : throw new BfShaderFormatException($"{glslName} is not a sampler name");

    [GeneratedRegex(@"^_[vf]p_c(\d+)$")]
    private static partial Regex BlockName();

    [GeneratedRegex(@"^[vf]p_t_tcb_([0-9A-Fa-f]+)$")]
    private static partial Regex SamplerName();
}
