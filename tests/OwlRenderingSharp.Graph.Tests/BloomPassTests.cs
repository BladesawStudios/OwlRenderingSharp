using OwlRenderingSharp.Gpu;

namespace OwlRenderingSharp.Graph.Tests;

[Collection(GlCollection.Name)]
public class BloomPassTests(GlFixture gl)
{
    private static float[] Bloom(GpuDevice device, float light, PostSettings settings)
    {
        using GpuTexture hdr = new(device, "hdr", GpuFormat.Rgba32F, 16, 16);
        float[] data = new float[16 * 16 * 4];
        for (int y = 6; y < 10; y++)
            for (int x = 6; x < 10; x++) data[(y * 16 + x) * 4] = data[(y * 16 + x) * 4 + 1] = data[(y * 16 + x) * 4 + 2] = light;
        hdr.Upload<float>(data);
        using BloomPass pass = new(device);
        using GpuTexture bloom = pass.Run(hdr, settings);
        Assert.Equal((4, 4), (bloom.Width, bloom.Height));
        float[] rgba = bloom.DownloadAsFloats();
        return [.. Enumerable.Range(0, 16).Select(i => rgba[i * 4])];
    }

    [SkippableFact]
    public void LightAboveTheThresholdSpreadsToTheNeighbouringTexels()
    {
        float[] pixels = gl.Require().Run(device => Bloom(device, 8f, new PostSettings(Exposure: 1f, BloomThreshold: 1f, BloomIntensity: 1f)));
        Assert.True(pixels[1 * 4 + 1] > 0.1f, "the block's own texel");
        Assert.True(pixels[1 * 4 + 0] > 0f, "a neighbour");
    }

    [SkippableFact]
    public void LightBelowTheThresholdMakesNoBloom()
    {
        float[] pixels = gl.Require().Run(device => Bloom(device, 0.5f, new PostSettings(Exposure: 1f, BloomThreshold: 1f, BloomIntensity: 1f)));
        Assert.All(pixels, p => Assert.Equal(0f, p, 1e-6f));
    }

    [SkippableFact]
    public void TheBloomClampLimitsHowBrightASampleCounts()
    {
        float[] pixels = gl.Require().Run(device => Bloom(device, 1000f, new PostSettings(Exposure: 1f, BloomThreshold: 1f, BloomIntensity: 1f, BloomClamp: 4f)));
        Assert.True(pixels.Max() <= 4f + 1e-3f, $"max {pixels.Max()}");
    }
}
