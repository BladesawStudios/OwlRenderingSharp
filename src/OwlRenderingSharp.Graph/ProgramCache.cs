using BfShaderSharp.Bnsh;
using BfShaderSharp.Errors;
using BfShaderSharp.Prepared;
using OwlRenderingSharp.Content;
using OwlRenderingSharp.Content.Programs;
using OwlRenderingSharp.Gpu;

namespace OwlRenderingSharp.Graph;

/// <summary>Compiles and links the programs of the prepared shader archives on first use and keeps them by shader archive, shading model and program number.</summary>
/// <remarks>
/// Each program is built from the decompiled GLSL of its stages and its blocks are moved to the points of <see cref="BlockBindingPoints"/>, a block the
/// compiler dropped being skipped. A program number the archive does not have throws <see cref="BfShaderFormatException"/>; a compile error is a
/// <see cref="GpuException"/> naming the program. It is used from the GL thread only and disposes what it made.
/// </remarks>
public sealed class ProgramCache(GpuDevice device, ContentSet content, BlockBindingPoints points) : IDisposable
{
    private readonly Dictionary<(string, string, int), ProgramEntry> _programs = [];

    public int Count => _programs.Count;

    public ProgramEntry Get(string archiveName, string shadingModel, int program)
    {
        (string, string, int) key = (archiveName, shadingModel, program);
        if (!_programs.TryGetValue(key, out ProgramEntry? entry)) _programs[key] = entry = Build(archiveName, shadingModel, program);
        return entry;
    }

    public void Dispose()
    {
        foreach (ProgramEntry entry in _programs.Values) entry.Program.Dispose();
        _programs.Clear();
    }

    private static string? Compatible(string? glsl) => glsl is null ? null : GlslCompatibility.Rewrite(glsl);

    private ProgramEntry Build(string archiveName, string shadingModel, int program)
    {
        PreparedModel model = content.Shaders(archiveName).Model(shadingModel) ?? throw new BfShaderFormatException($"{archiveName} has no shading model {shadingModel}");
        if (program < 0 || program >= model.ProgramCount) throw new BfShaderFormatException($"{archiveName}/{shadingModel} has no program {program}");
        string? vertex = Compatible(model.Glsl(program, ShaderStage.Vertex)), fragment = Compatible(model.Glsl(program, ShaderStage.Fragment));
        Dictionary<GpuShaderStage, string> sources = [];
        if (vertex is not null) sources[GpuShaderStage.Vertex] = vertex;
        if (fragment is not null) sources[GpuShaderStage.Fragment] = fragment;
        string name = $"{archiveName}/{shadingModel} program {program}";
        GpuProgram linked = new(device, name, sources);
        ProgramBindings bindings = ProgramBindings.For(model, program, vertex, fragment);
        foreach (BlockBinding block in bindings.Blocks) linked.TryBindUniformBlock(block.GlslName, (uint)points.Get(block.GsysName));
        return new ProgramEntry(
            linked,
            bindings,
            $"{archiveName}/{shadingModel}",
            model.Attributes().ToDictionary(a => a.Name, a => a.Location),
            model.Blocks().GroupBy(b => b.Name).ToDictionary(g => g.Key, g => g.Max(b => b.ByteSize)));
    }
}
