namespace OwlRenderingSharp.Gpu;

/// <summary>The pixel formats a texture or render target can have.</summary>
/// <remarks>The names are the renderer's; <see cref="GpuFormats"/> maps each to GL, so nothing above this project names a GL enum.</remarks>
public enum GpuFormat
{
    Rgba8,
    Rgba8Srgb,
    Rgba16F,
    Rgba32F,
    Rg16F,
    R8,
    R16F,
    R32F,
    R32Uint,
    Depth32F,
    Depth24Stencil8,
}
