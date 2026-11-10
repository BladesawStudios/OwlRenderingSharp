using System.Numerics;
using OwlRenderingSharp.Gpu;
using Silk.NET.OpenGL;

namespace OwlRenderingSharp.Graph.Tests;

[Collection(GlCollection.Name)]
public class ShadowPrepassTests(GlFixture gl)
{
    // The centre pixel of a depth image looks down the camera axis; the cascade maps every world point to (x + 0.5, y + 0.5, 0.25), so only the stored depth decides.
    private static float CentreVisibility(GpuDevice device, float stored, float depthValue = 0.5f)
    {
        Camera camera = new() { Eye = new Vector3(0, 0, 3), Target = Vector3.Zero };
        ShadowCascade cascade = new(new Camera(), Matrix4x4.CreateScale(1f, 1f, 0f) * Matrix4x4.CreateTranslation(0.5f, 0.5f, 0.25f), float.MaxValue);
        using GpuDepthArray array = new(device, "cascades", 8, 8, 1);
        using GpuDepthLayerTarget layer = new(device, "layer", array, 0);
        device.Apply(DrawState.Opaque);
        device.Gl.ClearNamedFramebuffer(layer.Handle, GLEnum.Depth, 0, [stored]);
        using GpuTexture depth = new(device, "view depth", GpuFormat.Depth32F, 9, 9);
        depth.Upload<float>(Enumerable.Repeat(depthValue, 81).ToArray());
        using ShadowPrepass prepass = new(device, 9, 9);
        prepass.Resolve(depth, camera, [cascade], array.Handle, 8);
        return prepass.ReadMask()[(4 * 9 + 4) * 4 + 1] / 255f;
    }

    [SkippableFact]
    public void APointBehindTheStoredDepthIsShadowedAndOneInFrontIsLit()
    {
        gl.Require().Run(device =>
        {
            Assert.Equal(0f, CentreVisibility(device, stored: 0.1f), 0.02f);
            Assert.Equal(1f, CentreVisibility(device, stored: 0.9f), 0.02f);
        });
    }

    [SkippableFact]
    public void PixelsWithNoGeometryAreLit()
    {
        gl.Require().Run(device => Assert.Equal(1f, CentreVisibility(device, stored: 0f, depthValue: 1f), 0.02f));
    }

    [SkippableFact]
    public void MoreCascadesThanTheMaskReadsAreRefusedByName()
    {
        gl.Require().Run(device =>
        {
            using GpuTexture depth = new(device, "view depth", GpuFormat.Depth32F, 4, 4);
            using ShadowPrepass prepass = new(device, 4, 4);
            ShadowCascade cascade = new(new Camera(), Matrix4x4.Identity, 1f);
            GpuException error = Assert.Throws<GpuException>(() => prepass.Resolve(depth, new Camera(), [cascade, cascade, cascade, cascade], 0, 8));
            Assert.Equal("shadow prepass", error.Resource);
        });
    }
}
