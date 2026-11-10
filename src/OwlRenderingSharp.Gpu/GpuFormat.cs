namespace OwlRenderingSharp.Gpu;

/// <summary>The pixel formats a texture or render target can have.</summary>
/// <remarks>The names are the renderer's; <see cref="GpuFormats"/> maps each to GL, so nothing above this project names a GL enum. The Bc formats are block compressed and are uploaded with <see cref="GpuTexture.UploadCompressed"/>.</remarks>
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
    Bc1,
    Bc1Srgb,
    Bc2,
    Bc2Srgb,
    Bc3,
    Bc3Srgb,
    Bc4,
    Bc4Snorm,
    Bc5,
    Bc5Snorm,
    Bc7,
    Bc7Srgb,
}
