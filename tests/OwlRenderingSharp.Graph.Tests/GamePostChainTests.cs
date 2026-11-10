using OwlRenderingSharp.Content;
using OwlRenderingSharp.Gpu;

namespace OwlRenderingSharp.Graph.Tests;

/// <summary>Runs the game's own post shaders; needs the prepared screen shader archive named by <c>OWL_AGL_SHADERS</c> (BfShaderSharp's <c>prepare</c> of the game's AglShader.sharcb).</summary>
[Collection(GlCollection.Name)]
public class GamePostChainTests(GlFixture gl)
{
    private static float[] Compose(GpuDevice device, float light, PostSettings settings)
    {
        string path = Environment.GetEnvironmentVariable("OWL_AGL_SHADERS")!;
        using ContentSet content = new(new CollectingDiagnostics());
        content.AddShaders(GamePostChain.Archive, path);
        using RenderServices services = new(device, content);
        using GpuTexture hdr = new(device, "hdr", GpuFormat.Rgba32F, 4, 4);
        hdr.Upload<float>(Enumerable.Range(0, 16).SelectMany(_ => new[] { light, light, light, 1f }).ToArray());
        using GamePostChain chain = new(services);
        using GpuTexture result = chain.Run(hdr, settings);
        return result.DownloadAsFloats()[..4];
    }

    // The filmic curve the game's compose program applies, to compare against.
    private static float Curve(float x) => Math.Clamp(x * (2.51f * x + 0.03f) / (x * (2.43f * x + 0.59f) + 0.14f), 0f, 1f);

    [SkippableFact]
    public void TheGamesComposeAppliesTheExposureAndTheFilmicCurveAndANeutralBalanceChangesNothing()
    {
        Skip.If(Environment.GetEnvironmentVariable("OWL_AGL_SHADERS") is null, "OWL_AGL_SHADERS is not set");
        float[] pixel = gl.Require().Run(device => Compose(device, 2f, new PostSettings(Exposure: 0.25f)));
        Assert.Equal(Curve(0.5f), pixel[0], 0.01f);
        Assert.Equal(pixel[0], pixel[1], 0.01f);
        Assert.Equal(pixel[0], pixel[2], 0.01f);
    }

    [SkippableFact]
    public void ACoolerWhitePointTintsTheImage()
    {
        Skip.If(Environment.GetEnvironmentVariable("OWL_AGL_SHADERS") is null, "OWL_AGL_SHADERS is not set");
        float[] pixel = gl.Require().Run(device => Compose(device, 2f, new PostSettings(Exposure: 0.25f, WhiteBalanceTemperature: 4500f)));
        Assert.True(pixel[2] > pixel[0], $"red {pixel[0]} blue {pixel[2]}");
    }
}
