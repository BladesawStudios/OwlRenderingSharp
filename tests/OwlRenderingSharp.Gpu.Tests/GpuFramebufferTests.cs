namespace OwlRenderingSharp.Gpu.Tests;

[Collection(GlCollection.Name)]
public class GpuFramebufferTests(GlFixture gl)
{
    [SkippableFact]
    public void ColorTargetsAndADepthTargetMakeACompleteFramebuffer()
    {
        gl.Require().Run(device =>
        {
            using GpuTexture albedo = new(device, "albedo", GpuFormat.Rgba8Srgb, 8, 8);
            using GpuTexture normal = new(device, "normal", GpuFormat.Rgba16F, 8, 8);
            using GpuTexture depth = new(device, "depth", GpuFormat.Depth32F, 8, 8);
            using GpuFramebuffer framebuffer = new(device, "gbuffer", [albedo, normal], depth);
            Assert.Equal((8, 8), (framebuffer.Width, framebuffer.Height));
        });
    }

    [SkippableFact]
    public void AttachmentsOfDifferentSizesAreRejectedWithTheName()
    {
        GpuException error = Assert.Throws<GpuException>(() => gl.Require().Run(device =>
        {
            using GpuTexture a = new(device, "a", GpuFormat.Rgba8, 8, 8);
            using GpuTexture b = new(device, "b", GpuFormat.Rgba8, 4, 4);
            new GpuFramebuffer(device, "mismatch", [a, b]);
        }));
        Assert.Equal("mismatch", error.Resource);
    }

    [SkippableFact]
    public void ADepthFormatAsColorIsRejected()
    {
        Assert.Throws<GpuException>(() => gl.Require().Run(device =>
        {
            using GpuTexture depth = new(device, "d", GpuFormat.Depth32F, 4, 4);
            new GpuFramebuffer(device, "wrong", [depth]);
        }));
    }

    [SkippableFact]
    public void AFramebufferWithNothingAttachedIsRejected()
    {
        Assert.Throws<GpuException>(() => gl.Require().Run(device => new GpuFramebuffer(device, "none", [])));
    }
}
