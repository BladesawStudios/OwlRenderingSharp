using BfShaderSharp.Errors;
using OwlRenderingSharp.Content.Geometry;

namespace OwlRenderingSharp.Content.Tests;

public class VertexDecoderTests
{
    private static float F(uint bits) => BitConverter.UInt32BitsToSingle(bits);

    private static uint[] Decode(uint word, params byte[] bytes)
    {
        uint[] output = new uint[4];
        VertexDecoder.Decode(VertexFormat.FromWord(word), bytes, output);
        return output;
    }

    [Fact]
    public void ThreeFloatsGetAWOfOne()
    {
        byte[] bytes = [.. BitConverter.GetBytes(1.5f), .. BitConverter.GetBytes(-2f), .. BitConverter.GetBytes(4f)];
        uint[] o = Decode(0x811, bytes);
        Assert.Equal((1.5f, -2f, 4f, 1f), (F(o[0]), F(o[1]), F(o[2]), F(o[3])));
    }

    [Fact]
    public void ATwoComponentWeightHasNoFullWeightFourthInfluence()
    {
        uint[] o = Decode(0x4, 128, 127);
        Assert.Equal(0f, F(o[2]));
        Assert.Equal(0f, F(o[3]));
    }

    [Fact]
    public void AnUnsignedNormalizedByteIsAFractionOf255()
    {
        uint[] o = Decode(0xa, 255, 0, 51, 255);
        Assert.Equal((1f, 0f, 0.2f, 1f), (F(o[0]), F(o[1]), F(o[2]), F(o[3])));
    }

    [Fact]
    public void ASignedNormalizedShortStaysInMinusOneToOne()
    {
        uint[] o = Decode(0x207, 0xff, 0x7f, 0x00, 0x80);
        Assert.Equal(1f, F(o[0]));
        Assert.Equal(-1f, F(o[1]));
    }

    [Fact]
    public void IntegerIndicesKeepTheirBitsAndMissingComponentsAreZero()
    {
        uint[] o = Decode(0x10a, 7, 0, 3, 200);
        Assert.Equal(new uint[] { 7, 0, 3, 200 }, o);
        uint[] two = Decode(0x104, 9, 4);
        Assert.Equal(new uint[] { 9, 4, 0, 0 }, two);
    }

    [Fact]
    public void AnIntegerLayoutWithTheFloatInterpretationConvertsTheValue()
    {
        uint[] o = Decode(0x808, 0x00, 0x3c, 0x00, 0x40);
        Assert.Equal((1f, 2f), (F(o[0]), F(o[1])));
        uint[] ints = Decode(0x80a, 1, 2, 3, 4);
        Assert.Equal((1f, 2f, 3f, 4f), (F(ints[0]), F(ints[1]), F(ints[2]), F(ints[3])));
    }

    [Fact]
    public void ASignedIntegerToFloatSignExtends()
    {
        Assert.Equal(-1f, F(Decode(0xa0a, 0xff, 0, 0, 0)[0]));
    }

    [Fact]
    public void ThePackedTenTenTenTwoSignedNormalizedWordHasAnUnsignedAlpha()
    {
        // x = 511 (max), y = -511, z = 0, w = 3.
        uint word = 511u | (uint)(-511 & 0x3ff) << 10 | 3u << 30;
        uint[] o = Decode(0x20b, BitConverter.GetBytes(word));
        Assert.Equal((1f, -1f, 0f, 1f), (F(o[0]), F(o[1]), F(o[2]), F(o[3])));
    }

    [Fact]
    public void FormatsReportTheirSizes()
    {
        Assert.Equal(12, VertexFormat.FromWord(0x811).ByteSize);
        Assert.Equal(4, VertexFormat.FromWord(0x20b).ByteSize);
        Assert.Equal(8, VertexFormat.FromWord(0x10e).ByteSize);
        Assert.Equal(2, VertexFormat.FromWord(0x104).ByteSize);
    }

    [Theory]
    [InlineData(0x4000u)]
    [InlineData(0x809u)]
    [InlineData(0x401u)]
    public void AnUnknownWordIsUnsupported(uint word)
    {
        Assert.Throws<BfShaderUnsupportedException>(() => VertexFormat.FromWord(word));
    }
}
