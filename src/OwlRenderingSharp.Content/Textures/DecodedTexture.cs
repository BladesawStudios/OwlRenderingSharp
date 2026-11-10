namespace OwlRenderingSharp.Content.Textures;

/// <summary>A 2D texture ready to upload: its size, encoding, the bytes of every mip level from the largest down and the channel sources it is meant to be read with.</summary>
/// <remarks>
/// Level bytes are in the encoding's own layout: rows of pixels for RGBA8, rows of 4x4 blocks for the BC encodings. Channels are the four sources the texture was stored with, red's in the low byte, numbered as BNTX
/// numbers them (zero 0, one 1, red 2, green 3, blue 4, alpha 5): a one-channel map is stored as (red, red, red, red) and a BC5 normal map as (red, green, zero, one).
/// </remarks>
public sealed record DecodedTexture(int Width, int Height, TextureEncoding Encoding, IReadOnlyList<byte[]> Levels, uint Channels = 0x05040302)
{
    public int MipCount => Levels.Count;
}
