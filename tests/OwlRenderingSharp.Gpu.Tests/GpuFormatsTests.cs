namespace OwlRenderingSharp.Gpu.Tests;

public class GpuFormatsTests
{
    [Fact]
    public void EveryFormatHasAGlMappingAndASize()
    {
        foreach (GpuFormat format in Enum.GetValues<GpuFormat>())
        {
            GpuFormats.Internal(format);
            if (GpuFormats.IsCompressed(format))
            {
                Assert.True(GpuFormats.BlockBytes(format) is 8 or 16, format.ToString());
                continue;
            }

            GpuFormats.Pixel(format);
            GpuFormats.Type(format);
            Assert.True(GpuTexture.PixelBytes(format) > 0, format.ToString());
        }
    }

    [Fact]
    public void OnlyTheDepthFormatsAreDepth()
    {
        Assert.Equal([GpuFormat.Depth32F, GpuFormat.Depth24Stencil8], Enum.GetValues<GpuFormat>().Where(GpuFormats.IsDepth));
    }
}
