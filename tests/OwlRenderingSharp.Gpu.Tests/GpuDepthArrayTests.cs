using Silk.NET.OpenGL;

namespace OwlRenderingSharp.Gpu.Tests;

[Collection(GlCollection.Name)]
public class GpuDepthArrayTests(GlFixture gl)
{
    [SkippableFact]
    public void EachLayerIsClearedThroughItsOwnTargetAndReadBackAlone()
    {
        gl.Require().Run(device =>
        {
            using GpuDepthArray array = new(device, "test cascades", 8, 4, 3);
            using GpuDepthLayerTarget first = new(device, "layer 0", array, 0), last = new(device, "layer 2", array, 2);
            device.Gl.ClearNamedFramebuffer(first.Handle, GLEnum.Depth, 0, [0.25f]);
            device.Gl.ClearNamedFramebuffer(last.Handle, GLEnum.Depth, 0, [0.75f]);
            device.Check();
            Assert.All(array.Download(0), d => Assert.Equal(0.25f, d));
            Assert.All(array.Download(2), d => Assert.Equal(0.75f, d));
            Assert.Equal(8 * 4, array.Download(1).Length);
        });
    }

    [SkippableFact]
    public void AMissingLayerIsRefusedByNameInsteadOfBeingBound()
    {
        gl.Require().Run(device =>
        {
            using GpuDepthArray array = new(device, "test cascades", 4, 4, 2);
            GpuException target = Assert.Throws<GpuException>(() => new GpuDepthLayerTarget(device, "layer 5", array, 5));
            Assert.Contains("layer 5", target.ToString());
            Assert.Throws<GpuException>(() => array.Download(2));
            Assert.Throws<GpuException>(() => new GpuDepthArray(device, "empty", 0, 4, 1));
        });
    }
}
