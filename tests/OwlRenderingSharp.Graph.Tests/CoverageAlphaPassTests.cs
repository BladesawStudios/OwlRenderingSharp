using OwlRenderingSharp.Gpu;

namespace OwlRenderingSharp.Graph.Tests;

[Collection(GlCollection.Name)]
public class CoverageAlphaPassTests(GlFixture gl)
{
    private float Alpha(float distance) => gl.Require().Run(device =>
    {
        using GpuTexture depth = SyntheticImages.Depth(device, distance);
        using GpuTexture mask = new(device, "mask", GpuFormat.Rgba8, SyntheticImages.Size, SyntheticImages.Size);
        using GpuTexture color = new(device, "color", GpuFormat.Rgba8, SyntheticImages.Size / 2, SyntheticImages.Size / 2);
        using GpuTexture result = new(device, "result", GpuFormat.Rgba8, SyntheticImages.Size / 2, SyntheticImages.Size / 2);
        using CoverageAlphaPass pass = new(device);
        using (GpuFramebuffer maskTarget = new(device, "mask", [mask])) pass.Mask(depth, maskTarget);
        using GpuFramebuffer target = new(device, "result", [result]);
        pass.Apply(color, mask, 2, target);
        return result.Download<byte>()[3] / 255f;
    });

    [SkippableFact]
    public void GeometryMakesAPixelOpaque() => Assert.Equal(1f, Alpha(2f), 0.01f);

    [SkippableFact]
    public void PixelsWithNoGeometryAreTransparent() => Assert.Equal(0f, Alpha(SyntheticImages.Far), 0.01f);
}
