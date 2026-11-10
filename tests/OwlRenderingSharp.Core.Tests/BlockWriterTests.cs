using System.Buffers.Binary;
using System.Numerics;
using OwlRenderingSharp.Blocks;

namespace OwlRenderingSharp.Core.Tests;

public class BlockWriterTests
{
    private static readonly BlockLayout Layout = new("b", 128,
    [
        new BlockField("view", 0, BlockFieldType.Matrix3x4, "test"),
        new BlockField("proj", 48, BlockFieldType.Matrix4x4, "test"),
        new BlockField("gain", 112, BlockFieldType.Float, "test"),
        new BlockField("flags", 116, BlockFieldType.UInt, "test"),
        new BlockField("count", 120, BlockFieldType.Int, "test"),
    ]);

    private static float F(BlockWriter w, int at) => BinaryPrimitives.ReadSingleLittleEndian(w.Bytes.AsSpan(at));

    [Fact]
    public void AMatrixIsWrittenAsTheRowsOfItsTranspose()
    {
        BlockWriter writer = new(Layout);
        writer.Set("view", Matrix4x4.CreateTranslation(1, 2, 3));
        Assert.Equal((1f, 2f, 3f), (F(writer, 12), F(writer, 28), F(writer, 44)));
        Assert.Equal(1f, F(writer, 0));
        Assert.Equal(0f, F(writer, 4));
    }

    [Fact]
    public void AThreeRowFieldWritesNothingBeyondItsRows()
    {
        BlockWriter writer = new(Layout);
        writer.Set("view", Matrix4x4.Identity);
        Assert.Equal(0f, F(writer, 48));
    }

    [Fact]
    public void ScalarsAreWrittenWhereTheLayoutSaysAndReadBack()
    {
        BlockWriter writer = new(Layout);
        writer.Set("gain", 2.5f).Set("flags", 7u).Set("count", -3);
        Assert.Equal(2.5f, writer.GetFloat("gain"));
        Assert.Equal(7u, BinaryPrimitives.ReadUInt32LittleEndian(writer.Bytes.AsSpan(116)));
        Assert.Equal(-3, BinaryPrimitives.ReadInt32LittleEndian(writer.Bytes.AsSpan(120)));
    }

    [Fact]
    public void AnUnknownFieldOrTheWrongTypeThrows()
    {
        BlockWriter writer = new(Layout);
        Assert.Throws<KeyNotFoundException>(() => writer.Set("nothing", 1f));
        Assert.Throws<ArgumentException>(() => writer.Set("gain", 1));
        Assert.Throws<ArgumentException>(() => writer.Set("gain", Matrix4x4.Identity));
        Assert.Throws<ArgumentException>(() => writer.Set("view", Vector4.One));
    }

    [Fact]
    public void ABlockCanStartFromACopyOfPrebuiltBytes()
    {
        byte[] start = new byte[128];
        start[1] = 9;
        BlockWriter writer = new(Layout, start);
        writer.Set("gain", 1f);
        Assert.Equal(9, writer.Bytes[1]);
        Assert.Equal(0, start[112]);
        Assert.Throws<ArgumentException>(() => new BlockWriter(Layout, new byte[16]));
    }
}
