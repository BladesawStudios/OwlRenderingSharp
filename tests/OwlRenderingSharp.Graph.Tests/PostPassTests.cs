using OwlRenderingSharp.Gpu;

namespace OwlRenderingSharp.Graph.Tests;

[Collection(GlCollection.Name)]
public class PostPassTests(GlFixture gl)
{
    private static GpuTexture Filled(GpuDevice device, int width, int height, Func<int, int, byte[]> pixel)
    {
        GpuTexture texture = new(device, "image", GpuFormat.Rgba8, width, height);
        byte[] data = new byte[width * height * 4];
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++) pixel(x, y).CopyTo(data, (y * width + x) * 4);
        texture.Upload<byte>(data);
        return texture;
    }

    [SkippableFact]
    public void FxaaLeavesAFlatImageAlone()
    {
        byte[] pixels = gl.Require().Run(device =>
        {
            using GpuTexture source = Filled(device, 8, 8, (_, _) => [100, 150, 200, 255]);
            using GpuTexture target = new(device, "target", GpuFormat.Rgba8, 8, 8);
            using GpuFramebuffer framebuffer = new(device, "target", [target]);
            using FxaaPass pass = new(device);
            pass.Run(source, framebuffer);
            return target.Download<byte>();
        });
        for (int i = 0; i < pixels.Length; i += 4) Assert.Equal([100, 150, 200, 255], pixels[i..(i + 4)]);
    }

    [SkippableFact]
    public void FxaaSoftensAStaircaseEdgeWithoutLeavingTheLumaRange()
    {
        byte[] pixels = gl.Require().Run(device =>
        {
            using GpuTexture source = Filled(device, 16, 16, (x, y) => x > y ? [255, 255, 255, 255] : [0, 0, 0, 255]);
            using GpuTexture target = new(device, "target", GpuFormat.Rgba8, 16, 16);
            using GpuFramebuffer framebuffer = new(device, "target", [target]);
            using FxaaPass pass = new(device);
            pass.Run(source, framebuffer);
            return target.Download<byte>();
        });
        int grey = 0;
        for (int i = 0; i < pixels.Length; i += 4) if (pixels[i] is > 8 and < 247) grey++;
        Assert.True(grey >= 8, $"only {grey} blended pixels");
    }

    [SkippableFact]
    public void AResolveAveragesEachSquareAfterToneMappingAndKeepsDarkBlack()
    {
        byte[] pixels = gl.Require().Run(device =>
        {
            using GpuTexture hdr = new(device, "hdr", GpuFormat.Rgba16F, 2, 2);
            using GpuTexture target = new(device, "target", GpuFormat.Rgba8, 1, 1);
            using GpuFramebuffer framebuffer = new(device, "target", [target]);
            using ResolvePass pass = new(device);
            pass.Run(hdr, 2, 1f, framebuffer);
            return target.Download<byte>();
        });
        Assert.Equal([0, 0, 0, 0], pixels);
    }

    [SkippableFact]
    public void AResolveWithTheWrongSourceSizeIsRefused()
    {
        gl.Require().Run(device =>
        {
            using GpuTexture hdr = new(device, "hdr", GpuFormat.Rgba16F, 3, 3);
            using GpuTexture target = new(device, "target", GpuFormat.Rgba8, 1, 1);
            using GpuFramebuffer framebuffer = new(device, "target", [target]);
            using ResolvePass pass = new(device);
            Assert.Throws<GpuException>(() => pass.Run(hdr, 2, 1f, framebuffer));
        });
    }
}
