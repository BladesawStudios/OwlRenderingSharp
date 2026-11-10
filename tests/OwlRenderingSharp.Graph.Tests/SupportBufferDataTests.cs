using System.Buffers.Binary;

namespace OwlRenderingSharp.Graph.Tests;

public class SupportBufferDataTests
{
    [Fact]
    public void TheBufferHasTheDeclaredSizeAndEveryRenderScaleIsOne()
    {
        byte[] bytes = SupportBufferData.Create(800, 600);
        Assert.Equal(SupportBufferData.Size, bytes.Length);
        for (int i = 0; i < 73; i++) Assert.Equal(1f, BinaryPrimitives.ReadSingleLittleEndian(bytes.AsSpan(192 + i * 16)));
    }

    [Fact]
    public void TheViewportIsWrittenWithItsInverse()
    {
        byte[] bytes = SupportBufferData.Create(800, 400);
        Assert.Equal(2f / 800, BinaryPrimitives.ReadSingleLittleEndian(bytes.AsSpan(144)));
        Assert.Equal(-2f / 400, BinaryPrimitives.ReadSingleLittleEndian(bytes.AsSpan(148)));
        Assert.Equal(800f, BinaryPrimitives.ReadSingleLittleEndian(bytes.AsSpan(160)));
        Assert.Equal(400f, BinaryPrimitives.ReadSingleLittleEndian(bytes.AsSpan(164)));
        Assert.Equal(1, BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(176)));
    }
}
