namespace OwlRenderingSharp.Gpu;

/// <summary>The fixed-function state of a draw: face culling and the depth test.</summary>
/// <remarks>Applied with <see cref="GpuDevice.Apply"/>. Blending is not part of it yet; draws are opaque.</remarks>
public readonly record struct DrawState(CullMode Cull, bool DepthTest, bool DepthWrite, DepthFunc DepthFunc)
{
    public static DrawState Opaque { get; } = new(CullMode.Back, true, true, Gpu.DepthFunc.LessOrEqual);
}
