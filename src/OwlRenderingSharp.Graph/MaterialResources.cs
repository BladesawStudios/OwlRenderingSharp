using OwlRenderingSharp.Gpu;

namespace OwlRenderingSharp.Graph;

/// <summary>Everything a material needs to draw: its program, draw state, uniform block and texture units.</summary>
/// <remarks>
/// The program, textures and samplers are shared through <see cref="RenderServices"/>; only the material block buffer belongs to this object, and it is
/// the only thing disposed. The block is null when the program has no material block.
/// </remarks>
public sealed class MaterialResources(string name, ProgramEntry program, DrawState state, GpuBuffer? block, IReadOnlyList<TextureUnit> units) : IDisposable
{
    public string Name { get; } = name;

    public ProgramEntry Program { get; } = program;

    public DrawState State { get; } = state;

    public GpuBuffer? Block { get; } = block;

    public IReadOnlyList<TextureUnit> Units { get; } = units;

    public void Dispose() => Block?.Dispose();
}
