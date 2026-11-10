using BfShaderSharp.Errors;
using BfShaderSharp.Pack.Records;
using TexSharp;

namespace OwlRenderingSharp.Content.Textures;

/// <summary>Turns a pack texture record and its linear level bytes into a <see cref="DecodedTexture"/>.</summary>
/// <remarks>
/// Only linear-storage 2D textures are read: a record's levels are consecutive in its blob, the largest first, and each level's size is in the
/// record. A format TexSharp does not read, a texture that is not a single 2D image, a level table that does not match the blob, or a native-storage
/// record throws <see cref="BfShaderFormatException"/> or <see cref="BfShaderUnsupportedException"/>.
/// </remarks>
public static class TextureDecoding
{
    public static DecodedTexture Decode(TextureRecord record, ReadOnlySpan<byte> data)
    {
        if (record.Storage != TextureStorage.Linear) throw new BfShaderUnsupportedException("Only linear texture storage is read; prepare the pack with linear textures");
        // TODO: array and cube textures are not read; they are rejected so that a partial upload never happens.
        if (record.ArrayLength > 1 || record.Depth > 1) throw new BfShaderUnsupportedException($"A texture with array length {record.ArrayLength} and depth {record.Depth} is not read");
        if (!BntxFormats.TryFromBntx(record.Format, out TextureFormat format, out bool srgb, out bool snorm))
            throw new BfShaderUnsupportedException($"Texture format 0x{record.Format:x} is not read");
        if (record.LevelSizes.Count == 0 || record.LevelSizes.Count != record.MipCount) throw new BfShaderFormatException("The texture record has no level sizes or too few");

        TextureEncoding? passThrough = Native(format, srgb, snorm);
        List<byte[]> levels = [];
        int offset = 0;
        for (int level = 0; level < record.MipCount; level++)
        {
            int width = Math.Max(1, record.Width >> level), height = Math.Max(1, record.Height >> level);
            int size = checked((int)record.LevelSizes[level]);
            if (size < format.LevelSize(width, height) || offset + size > data.Length) throw new BfShaderFormatException($"Texture level {level} ({width}x{height}, {size} bytes at {offset}, needs {format.LevelSize(width, height)}) does not fit its blob of {data.Length} bytes");
            ReadOnlySpan<byte> bytes = data.Slice(offset, size);
            levels.Add(passThrough is null ? TextureDecoder.ToRgba8(format, bytes, width, height, snorm) : bytes[..format.LevelSize(width, height)].ToArray());
            offset += size;
        }

        return new DecodedTexture(record.Width, record.Height, passThrough ?? (srgb ? TextureEncoding.Rgba8Srgb : TextureEncoding.Rgba8), levels);
    }

    // The encodings the GPU reads as they are. BC6H and the signed BC1-3 and BC7 variants GL has no format for are decoded instead.
    private static TextureEncoding? Native(TextureFormat format, bool srgb, bool snorm) => format switch
    {
        TextureFormat.Bc1 when !snorm => srgb ? TextureEncoding.Bc1Srgb : TextureEncoding.Bc1,
        TextureFormat.Bc2 when !snorm => srgb ? TextureEncoding.Bc2Srgb : TextureEncoding.Bc2,
        TextureFormat.Bc3 when !snorm => srgb ? TextureEncoding.Bc3Srgb : TextureEncoding.Bc3,
        TextureFormat.Bc4 => snorm ? TextureEncoding.Bc4Snorm : TextureEncoding.Bc4,
        TextureFormat.Bc5 => snorm ? TextureEncoding.Bc5Snorm : TextureEncoding.Bc5,
        TextureFormat.Bc7 when !snorm => srgb ? TextureEncoding.Bc7Srgb : TextureEncoding.Bc7,
        _ => null,
    };
}
