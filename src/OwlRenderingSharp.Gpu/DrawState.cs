namespace OwlRenderingSharp.Gpu;

/// <summary>The fixed-function state of a draw: culling, the depth test, blending, the colour channels written, the stencil test and the rasteriser settings.</summary>
/// <remarks>Applied with <see cref="GpuDevice.Apply"/>. Everything after the blend state defaults to the ordinary value.</remarks>
public readonly record struct DrawState(
    CullMode Cull,
    bool DepthTest,
    bool DepthWrite,
    DepthFunc DepthFunc,
    BlendState Blend,
    ColorMask Channels = ColorMask.All,
    RasterState Raster = default,
    StencilState Stencil = default)
{
    public static DrawState Opaque { get; } = new(CullMode.Back, true, true, Gpu.DepthFunc.LessOrEqual, BlendState.Off);
}
