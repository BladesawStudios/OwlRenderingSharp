using OwlRenderingSharp.Gpu;

namespace OwlRenderingSharp.Graph.Tests;

[Collection(GlCollection.Name)]
public class SubsurfacePassTests(GlFixture gl)
{
    private static readonly SubsurfacePass.Settings Wide = new(WidthRed: 0.05f, WidthGreen: 0.05f, WidthBlue: 0.05f, Strength: 1f);

    // A white texel on black at distance 1; the skin depth image either matches the view depth (skin) or is empty (not skin).
    private (float Centre, float Beside) Scatter(bool skin) => gl.Require().Run(device =>
    {
        using GpuTexture lit = SyntheticImages.Lit(device, dot: true);
        using GpuTexture depth = SyntheticImages.Depth(device, 1f);
        using GpuTexture skinDepth = SyntheticImages.Depth(device, skin ? 1f : SyntheticImages.Far);
        using GpuTexture result = new(device, "result", GpuFormat.Rgba16F, SyntheticImages.Size, SyntheticImages.Size);
        using GpuFramebuffer target = new(device, "result", [result]);
        using SubsurfacePass pass = new(device);
        pass.Run(lit, depth, skinDepth, SyntheticImages.Camera, Wide, target);
        return (SyntheticImages.Red(result, 16, 16), SyntheticImages.Red(result, 18, 16));
    });

    [SkippableFact]
    public void LightSpreadsAcrossSkinPixels()
    {
        (float centre, float beside) = Scatter(skin: true);
        Assert.True(centre < 0.9f, $"centre {centre}");
        Assert.True(beside > 0.01f, $"beside {beside}");
    }

    [SkippableFact]
    public void PixelsThatAreNotSkinAreLeftAlone()
    {
        (float centre, float beside) = Scatter(skin: false);
        Assert.Equal(1f, centre, 1e-3f);
        Assert.Equal(0f, beside, 1e-3f);
    }
}
