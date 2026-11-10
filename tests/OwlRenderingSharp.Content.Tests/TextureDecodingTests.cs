using BfShaderSharp.Errors;
using BfShaderSharp.Pack;
using BfShaderSharp.Pack.Records;
using OwlRenderingSharp.Content.Textures;

namespace OwlRenderingSharp.Content.Tests;

public class TextureDecodingTests
{
    private const uint Rgba8Unorm = 0x0b01, Bc1Srgb = 0x1a06, Bc4Unorm = 0x1d01, Astc4x4Srgb = 0x2d06;

    private static TextureRecord Record(uint format, int width, int height, uint[] levels, TextureStorage storage = TextureStorage.Linear) =>
        new(default, 0, format, width, height, 1, 1, levels.Length, storage, levels);

    [Fact]
    public void AnRgbaTextureKeepsEveryLevelInOrder()
    {
        byte[] data = [.. Enumerable.Repeat((byte)1, 16), .. Enumerable.Repeat((byte)2, 4)];
        DecodedTexture texture = TextureDecoding.Decode(Record(Rgba8Unorm, 2, 2, [16, 4]), data);
        Assert.Equal(TextureEncoding.Rgba8, texture.Encoding);
        Assert.Equal(2, texture.MipCount);
        Assert.Equal(new byte[] { 1, 1, 1, 1 }, texture.Levels[0][..4]);
        Assert.Equal(new byte[] { 2, 2, 2, 2 }, texture.Levels[1]);
    }

    [Fact]
    public void ABlockCompressedTextureIsPassedThroughAsStored()
    {
        byte[] blocks = [1, 2, 3, 4, 5, 6, 7, 8];
        DecodedTexture texture = TextureDecoding.Decode(Record(Bc1Srgb, 4, 4, [8]), blocks);
        Assert.Equal(TextureEncoding.Bc1Srgb, texture.Encoding);
        Assert.Equal(blocks, texture.Levels[0]);
    }

    [Fact]
    public void ASingleChannelBlockFormatKeepsItsOwnEncoding()
    {
        Assert.Equal(TextureEncoding.Bc4, TextureDecoding.Decode(Record(Bc4Unorm, 4, 4, [8]), new byte[8]).Encoding);
    }

    [Fact]
    public void AnAstcTextureIsDecodedToRgbaWithItsColorSpace()
    {
        // A void-extent block: one colour for the whole block, here red with full alpha.
        byte[] block = [0xfc, 0xfd, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0x00, 0x00, 0x00, 0x00, 0xff, 0xff];
        DecodedTexture texture = TextureDecoding.Decode(Record(Astc4x4Srgb, 4, 4, [16]), block);
        Assert.Equal(TextureEncoding.Rgba8Srgb, texture.Encoding);
        Assert.Equal(64, texture.Levels[0].Length);
        Assert.Equal(new byte[] { 255, 0, 0, 255 }, texture.Levels[0][..4]);
    }

    [Fact]
    public void ANativeStorageRecordIsUnsupported()
    {
        Assert.Throws<BfShaderUnsupportedException>(() => TextureDecoding.Decode(Record(Rgba8Unorm, 2, 2, [16], TextureStorage.Native), new byte[16]));
    }

    [Fact]
    public void AnArrayTextureIsUnsupported()
    {
        TextureRecord array = Record(Rgba8Unorm, 2, 2, [16]) with { ArrayLength = 6 };
        Assert.Throws<BfShaderUnsupportedException>(() => TextureDecoding.Decode(array, new byte[96]));
    }

    [Fact]
    public void ALevelPastTheEndOfTheBlobIsMalformed()
    {
        Assert.Throws<BfShaderFormatException>(() => TextureDecoding.Decode(Record(Rgba8Unorm, 2, 2, [16]), new byte[8]));
    }

    [Fact]
    public void AnUnknownFormatIsUnsupported()
    {
        Assert.Throws<BfShaderUnsupportedException>(() => TextureDecoding.Decode(Record(0xffff01, 2, 2, [16]), new byte[16]));
    }
}
