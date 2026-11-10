using BfShaderSharp.Pack.Records;
using OwlRenderingSharp.Gpu;

namespace OwlRenderingSharp.Graph;

/// <summary>Everything a material needs to draw: its draw state, its uniform block and, per pass and skin weight count, a program with its texture units.</summary>
/// <remarks>
/// <see cref="Program"/> is the program the pack recorded; geometry is laid out for its attributes. The program of a pass is chosen on first use by the
/// <see cref="PassProgramSelector"/> and kept, and is null when the shading model has no such program. Programs, textures and samplers are shared through
/// <see cref="RenderServices"/>; only the material block buffer belongs to this object, and it is the only thing disposed. The block is null when the program has no material block.
/// </remarks>
public sealed class MaterialResources(string name, MaterialRecord record, ProgramEntry program, DrawState state, GpuBuffer? block, MaterialResourcesBuilder builder) : IDisposable
{
    private readonly Dictionary<(RenderPass, int), MaterialPass?> _passes = [];

    public string Name { get; } = name;

    public MaterialRecord Record { get; } = record;

    public ProgramEntry Program { get; } = program;

    public DrawState State { get; } = state;

    public GpuBuffer? Block { get; } = block;

    // Whether the material blends, which puts it after the opaque ones; and its order among its kind.
    public bool Blends { get; } = MaterialDrawStates.Blends(record);

    public int Priority { get; } = MaterialDrawStates.Priority(record);

    public AlphaTest AlphaTest { get; } = MaterialDrawStates.AlphaTest(record);

    public MaterialPass? Pass(RenderPass pass, int skinCount)
    {
        if (!_passes.TryGetValue((pass, skinCount), out MaterialPass? found)) _passes[(pass, skinCount)] = found = builder.CreatePass(Name, Record, pass, skinCount);
        return found;
    }

    public void Dispose() => Block?.Dispose();
}
