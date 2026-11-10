using OwlRenderingSharp.Content;
using OwlRenderingSharp.Gpu;

namespace OwlRenderingSharp.Graph.Tests;

/// <summary>Runs the game's occlusion compute programs; needs the prepared screen shader archive named by <c>OWL_AGL_SHADERS</c>.</summary>
[Collection(GlCollection.Name)]
public class SsaoChainTests(GlFixture gl)
{
    private const int Size = 64;

    // A wall four metres ahead and a floor one metre below the eye, seen at ninety degrees: their crease runs across the lower part of the image.
    private float[] Occlusion()
    {
        return gl.Require().Run(device =>
        {
            string path = Environment.GetEnvironmentVariable("OWL_AGL_SHADERS")!;
            using ContentSet content = new(new CollectingDiagnostics());
            content.AddShaders(GamePostChain.Archive, path);
            using RenderServices services = new(device, content);
            Camera camera = new() { NearPlane = 0.1f, FarPlane = 100f, FovRadians = MathF.PI / 2f };
            float[] stored = new float[Size * Size];
            for (int y = 0; y < Size; y++)
                for (int x = 0; x < Size; x++)
                {
                    float up = 1f - (y + 0.5f) / Size * 2f;
                    float distance = up < 0f ? Math.Min(4f, 1f / -up) : 4f;
                    stored[(Size - 1 - y) * Size + x] = (1f - 0.1f / distance) / (1f - 0.1f / 100f);
                }
            using GpuTexture depth = new(device, "depth", GpuFormat.Depth32F, Size, Size);
            depth.Upload<float>(stored);
            using SsaoChain chain = new(services);
            using GpuTexture result = chain.Run(depth, camera, new SsaoSettings(Radius: 1f, MaxRadiusUv: 0.2f));
            float[] rgba = result.DownloadAsFloats();
            // Rows come back bottom first.
            rgba = Enumerable.Range(0, Size).SelectMany(y => rgba.Skip((Size - 1 - y) * Size * 4).Take(Size * 4)).ToArray();
            return Enumerable.Range(0, Size * Size).Select(i => rgba[i * 4 + int.Parse(Environment.GetEnvironmentVariable("AO_CH") ?? "0")]).ToArray();
        });
    }

    [SkippableFact]
    public void TheCreaseBetweenAWallAndAFloorIsDarkerThanTheOpenWall()
    {
        Skip.If(Environment.GetEnvironmentVariable("OWL_AGL_SHADERS") is null, "OWL_AGL_SHADERS is not set");
        float[] ao = Occlusion();
                float wall = ao[8 * Size + 32], crease = ao[(Size / 2 + 3) * Size + 32];
        Assert.True(wall > 0.95f, $"open wall {wall}");
        Assert.True(crease < wall - 0.05f, $"crease {crease} wall {wall}");
    }
}
