using System.Numerics;
using OwlRenderingSharp.Gpu;

namespace OwlRenderingSharp.Graph.Tests;

[Collection(GlCollection.Name)]
public class DepthFogPassTests(GlFixture gl)
{
    private float Fogged(float distance, Vector4 fog) => gl.Require().Run(device =>
    {
        using GpuTexture lit = SyntheticImages.Lit(device, dot: false);
        using GpuTexture depth = SyntheticImages.Depth(device, distance);
        using GpuTexture result = new(device, "result", GpuFormat.Rgba16F, SyntheticImages.Size, SyntheticImages.Size);
        using GpuFramebuffer target = new(device, "result", [result]);
        using DepthFogPass pass = new(device);
        pass.Run(lit, depth, SyntheticImages.Camera, fog, 5f, 15f, target);
        return SyntheticImages.Red(result, 8, 8);
    });

    [SkippableFact]
    public void TheFogFractionRisesLinearlyBetweenItsStartAndEnd()
    {
        Assert.Equal(1f, Fogged(2f, new Vector4(0, 0, 0, 1)), 0.01f);
        Assert.Equal(0.5f, Fogged(10f, new Vector4(0, 0, 0, 1)), 0.02f);
        Assert.Equal(0f, Fogged(30f, new Vector4(0, 0, 0, 1)), 0.01f);
    }

    [SkippableFact]
    public void AClearFogColourLeavesTheImageAlone() => Assert.Equal(1f, Fogged(30f, Vector4.Zero), 0.01f);

    [SkippableFact]
    public void PixelsWithNoGeometryAreNotFogged() => Assert.Equal(1f, Fogged(SyntheticImages.Far, new Vector4(0, 0, 0, 1)), 0.01f);
}
