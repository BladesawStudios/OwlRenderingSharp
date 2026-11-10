using OwlRenderingSharp.Content.Textures;

namespace OwlRenderingSharp.Gpu;

/// <summary>Creates a <see cref="GpuTexture"/> from a <see cref="DecodedTexture"/> and uploads every mip level.</summary>
/// <remarks>The texture has the decoded texture's mip count and no mips are generated; block-compressed levels go up as the game stored them.</remarks>
public static class GpuTextureLoader
{
    public static GpuTexture Create(GpuDevice device, string name, DecodedTexture texture)
    {
        GpuFormat format = FormatOf(texture.Encoding);
        GpuTexture gpu = new(device, name, format, texture.Width, texture.Height, texture.MipCount);
        for (int level = 0; level < texture.MipCount; level++)
        {
            if (GpuFormats.IsCompressed(format)) gpu.UploadCompressed(texture.Levels[level], level);
            else gpu.Upload<byte>(texture.Levels[level], level);
        }

        return gpu;
    }

    public static GpuFormat FormatOf(TextureEncoding encoding) => encoding switch
    {
        TextureEncoding.Rgba8 => GpuFormat.Rgba8,
        TextureEncoding.Rgba8Srgb => GpuFormat.Rgba8Srgb,
        TextureEncoding.Bc1 => GpuFormat.Bc1,
        TextureEncoding.Bc1Srgb => GpuFormat.Bc1Srgb,
        TextureEncoding.Bc2 => GpuFormat.Bc2,
        TextureEncoding.Bc2Srgb => GpuFormat.Bc2Srgb,
        TextureEncoding.Bc3 => GpuFormat.Bc3,
        TextureEncoding.Bc3Srgb => GpuFormat.Bc3Srgb,
        TextureEncoding.Bc4 => GpuFormat.Bc4,
        TextureEncoding.Bc4Snorm => GpuFormat.Bc4Snorm,
        TextureEncoding.Bc5 => GpuFormat.Bc5,
        TextureEncoding.Bc5Snorm => GpuFormat.Bc5Snorm,
        TextureEncoding.Bc7 => GpuFormat.Bc7,
        TextureEncoding.Bc7Srgb => GpuFormat.Bc7Srgb,
        _ => throw new ArgumentOutOfRangeException(nameof(encoding)),
    };
}
