namespace OwlRenderingSharp.Gpu;

/// <summary>The fixed-function state of a draw: face culling, the depth test, blending and whether colour is written.</summary>
/// <remarks>Applied with <see cref="GpuDevice.Apply"/>.</remarks>
public readonly record struct DrawState(CullMode Cull, bool DepthTest, bool DepthWrite, DepthFunc DepthFunc, BlendState Blend, bool ColorWrite = true)
{
    public static DrawState Opaque { get; } = new(CullMode.Back, true, true, Gpu.DepthFunc.LessOrEqual, BlendState.Off);
}
