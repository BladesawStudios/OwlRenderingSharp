namespace OwlRenderingSharp.Gpu;

/// <summary>The programmable stages a <see cref="GpuProgram"/> can be linked from.</summary>
/// <remarks>Compute programs are linked from a compute stage alone; the others from a vertex stage and usually a fragment stage.</remarks>
public enum GpuShaderStage
{
    Vertex,
    Geometry,
    Fragment,
    Compute,
}
