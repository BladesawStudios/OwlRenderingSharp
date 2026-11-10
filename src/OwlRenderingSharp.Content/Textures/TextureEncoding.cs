namespace OwlRenderingSharp.Content.Textures;

/// <summary>How the bytes of a decoded texture level are laid out.</summary>
/// <remarks>
/// The block-compressed encodings are the ones every desktop GPU reads natively and are passed through as the game stored them. Everything else
/// (ASTC, packed and wide formats) is decoded to <see cref="Rgba8"/> or <see cref="Rgba8Srgb"/> on the CPU.
/// </remarks>
public enum TextureEncoding
{
    Rgba8,
    Rgba8Srgb,
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
