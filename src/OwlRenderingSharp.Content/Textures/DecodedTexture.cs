namespace OwlRenderingSharp.Content.Textures;

/// <summary>A 2D texture ready to upload: its size, encoding and the bytes of every mip level from the largest down.</summary>
/// <remarks>Level bytes are in the encoding's own layout: rows of pixels for RGBA8, rows of 4x4 blocks for the BC encodings.</remarks>
public sealed record DecodedTexture(int Width, int Height, TextureEncoding Encoding, IReadOnlyList<byte[]> Levels)
{
    public int MipCount => Levels.Count;
}
