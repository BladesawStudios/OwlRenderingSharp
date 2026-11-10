using OwlRenderingSharp.Gpu;

namespace OwlRenderingSharp.Graph.Tests;

[Collection(GlCollection.Name)]
public class ViewDepthBuffersTests(GlFixture gl)
{
    private static float Linear(GpuDevice device, float stored, Camera camera)
    {
        using GpuTexture depth = new(device, "view depth", GpuFormat.Depth32F, 4, 4);
        depth.Upload<float>(Enumerable.Repeat(stored, 16).ToArray());
        using ViewDepthBuffers buffers = new(device, 4, 4);
        buffers.Resolve(depth, camera);
        return buffers.ReadLinear()[5];
    }

    [SkippableFact]
    public void TheLinearImageHoldsTheViewDistanceOverTheFarPlane()
    {
        gl.Require().Run(device =>
        {
            Camera camera = new() { NearPlane = 1f, FarPlane = 101f };
            Assert.Equal(1f / 101f, Linear(device, 0f, camera), 1e-4f);
            Assert.Equal(1f / (1f - 0.5f * (1f - 1f / 101f)) / 101f, Linear(device, 0.5f, camera), 1e-4f);
        });
    }

    [SkippableFact]
    public void PixelsWithNoGeometryReadOne()
    {
        gl.Require().Run(device => Assert.Equal(1f, Linear(device, 1f, new Camera { NearPlane = 1f, FarPlane = 101f }), 1e-4f));
    }

    [SkippableFact]
    public void TheFrameOffersBothImagesByTheirEngineNames()
    {
        gl.Require().Run(device =>
        {
            using ViewDepthBuffers buffers = new(device, 4, 4);
            Assert.Contains(ViewDepthBuffers.DepthName, buffers.Textures.Keys);
            Assert.Contains(ViewDepthBuffers.LinearName, buffers.Textures.Keys);
        });
    }
}
