using Silk.NET.OpenGL;

namespace OwlRenderingSharp.Gpu.Tests;

[Collection(GlCollection.Name)]
public class GpuBufferTests(GlFixture gl)
{
    [SkippableFact]
    public void WhatIsWrittenComesBack()
    {
        float[] back = gl.Require().Run(device =>
        {
            using GpuBuffer buffer = new(device, "test", 16);
            buffer.Write<float>([1, 2, 3, 4]);
            float[] result = new float[4];
            buffer.Read<float>(result);
            return result;
        });
        Assert.Equal([1f, 2f, 3f, 4f], back);
    }

    [SkippableFact]
    public void AWriteAtAnOffsetLeavesTheRestAlone()
    {
        float[] back = gl.Require().Run(device =>
        {
            using GpuBuffer buffer = new(device, "test", 16);
            buffer.Write<float>([1, 2, 3, 4]);
            buffer.Write<float>([9], 8);
            float[] result = new float[4];
            buffer.Read<float>(result);
            return result;
        });
        Assert.Equal([1f, 2f, 9f, 4f], back);
    }

    [SkippableFact]
    public void AWritePastTheEndNamesTheBuffer()
    {
        GpuException error = Assert.Throws<GpuException>(() => gl.Require().Run(device =>
        {
            using GpuBuffer buffer = new(device, "camera", 8);
            buffer.Write<float>([1, 2, 3]);
        }));
        Assert.Equal("camera", error.Resource);
    }

    [SkippableFact]
    public void ADeviceCheckListsThePendingErrorsWithThePass()
    {
        GpuException error = Assert.Throws<GpuException>(() => gl.Require().Run(device =>
        {
            device.Gl.BindTexture(TextureTarget.Texture2D, 0xfffff);
            device.Check("probe", "texture");
        }));
        Assert.Equal("probe", error.Pass);
        Assert.Contains("InvalidOperation", error.Message);
    }
}
