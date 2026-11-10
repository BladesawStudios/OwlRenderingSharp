using OwlRenderingSharp.Gpu;

namespace OwlRenderingSharp.Graph.Tests;

[Collection(GlCollection.Name)]
public class AmbientOcclusionPassTests(GlFixture gl)
{
    private const int Size = 32;

    // A white image over a depth image with a step: the left half is farther (distance 2) and the right half nearer (1.85).
    private static float[] Run(GpuDevice device, float strength, bool background = false, bool skin = false)
    {
        Camera camera = new() { NearPlane = 1f, FarPlane = 101f };
        using GpuTexture lit = new(device, "lit", GpuFormat.Rgba16F, Size, Size);
        using GpuFramebuffer fill = new(device, "lit", [lit]);
        using GpuScreenPass pass = new(device, "fill", "#version 450 core\nin vec2 uv;\nout vec4 color;\nvoid main() { color = vec4(1.0); }\n");
        pass.Run(fill, Array.Empty<uint>());
        using GpuTexture depth = new(device, "depth", GpuFormat.Depth32F, Size, Size);
        float far = background ? 1f : 0.505f, near = background ? 1f : 0.46f;
        depth.Upload<float>(Enumerable.Range(0, Size * Size).Select(i => i % Size < Size / 2 ? far : near).ToArray());
        using GpuTexture result = new(device, "result", GpuFormat.Rgba16F, Size, Size);
        using GpuFramebuffer target = new(device, "result", [result]);
        using AmbientOcclusionPass occlusion = new(device);
        occlusion.Run(lit, depth, camera, 1f, strength, target, skin ? depth : null);
        return result.DownloadAsFloats();
    }

    private static float Red(float[] pixels, int x, int y) => pixels[(y * Size + x) * 4];

    [SkippableFact]
    public void ThePixelsBesideACloserSurfaceAreDarkenedAndTheCloserSideIsNot()
    {
        float[] pixels = gl.Require().Run(device => Run(device, 0.8f));
        Assert.True(Red(pixels, 15, 16) < 0.95f, "the far side of the step is crowded");
        Assert.Equal(1f, Red(pixels, 20, 16), 1e-3f);
        Assert.Equal(1f, Red(pixels, 2, 16), 1e-3f);
    }

    [SkippableFact]
    public void SkinPixelsAreLeftAloneWhenTheSkinDepthIsGiven()
    {
        float[] pixels = gl.Require().Run(device => Run(device, 0.8f, skin: true));
        Assert.All(Enumerable.Range(0, Size * Size), i => Assert.Equal(1f, pixels[i * 4], 1e-3f));
    }

    [SkippableFact]
    public void NoStrengthLeavesTheImageAlone()
    {
        float[] pixels = gl.Require().Run(device => Run(device, 0f));
        Assert.All(Enumerable.Range(0, Size * Size), i => Assert.Equal(1f, pixels[i * 4], 1e-3f));
    }

    [SkippableFact]
    public void PixelsWithNoGeometryAreUntouched()
    {
        float[] pixels = gl.Require().Run(device => Run(device, 1f, background: true));
        Assert.All(Enumerable.Range(0, Size * Size), i => Assert.Equal(1f, pixels[i * 4], 1e-3f));
    }
}
