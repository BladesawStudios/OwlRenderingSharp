using Silk.NET.OpenGL;

namespace OwlRenderingSharp.Gpu;

/// <summary>Maps a <see cref="GpuFormat"/> to the GL enums that describe it.</summary>
/// <remarks>It is the single place a format becomes an internal format, an upload format, a pixel type and a byte size per pixel.</remarks>
public static class GpuFormats
{
    public static SizedInternalFormat Internal(GpuFormat format) => (SizedInternalFormat)(format switch
    {
        GpuFormat.Rgba8 => GLEnum.Rgba8,
        GpuFormat.Rgba8Srgb => GLEnum.Srgb8Alpha8,
        GpuFormat.Rgba16F => GLEnum.Rgba16f,
        GpuFormat.Rgba32F => GLEnum.Rgba32f,
        GpuFormat.Rg16F => GLEnum.RG16f,
        GpuFormat.R8 => GLEnum.R8,
        GpuFormat.R16F => GLEnum.R16f,
        GpuFormat.R32F => GLEnum.R32f,
        GpuFormat.R32Uint => GLEnum.R32ui,
        GpuFormat.Depth32F => GLEnum.DepthComponent32f,
        GpuFormat.Depth24Stencil8 => GLEnum.Depth24Stencil8,
        _ => throw new ArgumentOutOfRangeException(nameof(format)),
    });

    public static PixelFormat Pixel(GpuFormat format) => format switch
    {
        GpuFormat.Rgba8 or GpuFormat.Rgba8Srgb or GpuFormat.Rgba16F or GpuFormat.Rgba32F => PixelFormat.Rgba,
        GpuFormat.Rg16F => PixelFormat.RG,
        GpuFormat.R8 or GpuFormat.R16F or GpuFormat.R32F => PixelFormat.Red,
        GpuFormat.R32Uint => PixelFormat.RedInteger,
        GpuFormat.Depth32F => PixelFormat.DepthComponent,
        GpuFormat.Depth24Stencil8 => PixelFormat.DepthStencil,
        _ => throw new ArgumentOutOfRangeException(nameof(format)),
    };

    public static PixelType Type(GpuFormat format) => format switch
    {
        GpuFormat.Rgba8 or GpuFormat.Rgba8Srgb or GpuFormat.R8 => PixelType.UnsignedByte,
        GpuFormat.Rgba16F or GpuFormat.Rg16F or GpuFormat.R16F or GpuFormat.Rgba32F or GpuFormat.R32F or GpuFormat.Depth32F => PixelType.Float,
        GpuFormat.R32Uint => PixelType.UnsignedInt,
        GpuFormat.Depth24Stencil8 => PixelType.UnsignedInt248,
        _ => throw new ArgumentOutOfRangeException(nameof(format)),
    };

    public static bool IsDepth(GpuFormat format) => format is GpuFormat.Depth32F or GpuFormat.Depth24Stencil8;

    public static FramebufferAttachment DepthAttachment(GpuFormat format) =>
        format == GpuFormat.Depth24Stencil8 ? FramebufferAttachment.DepthStencilAttachment : FramebufferAttachment.DepthAttachment;
}
