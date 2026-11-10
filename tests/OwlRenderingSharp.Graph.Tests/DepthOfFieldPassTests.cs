using OwlRenderingSharp.Gpu;

namespace OwlRenderingSharp.Graph.Tests;

[Collection(GlCollection.Name)]
public class DepthOfFieldPassTests(GlFixture gl)
{
    private float Centre(float distance) => gl.Require().Run(device =>
    {
        using GpuTexture lit = SyntheticImages.Lit(device, dot: true);
        using GpuTexture depth = SyntheticImages.Depth(device, distance);
        using GpuTexture result = new(device, "result", GpuFormat.Rgba16F, SyntheticImages.Size, SyntheticImages.Size);
        using GpuFramebuffer target = new(device, "result", [result]);
        using DepthOfFieldPass pass = new(device);
        pass.Run(lit, depth, SyntheticImages.Camera, 10f, 5f, 6f, target);
        return SyntheticImages.Red(result, 16, 16);
    });

    [SkippableFact]
    public void ADistanceWithinTheRangeOfTheFocalDistanceStaysSharp() => Assert.Equal(1f, Centre(12f), 0.01f);

    [SkippableFact]
    public void ADistanceFarBeyondTheRangeIsSpreadOut() => Assert.True(Centre(40f) < 0.5f);
}
