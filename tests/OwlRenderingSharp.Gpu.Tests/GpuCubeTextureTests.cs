using Silk.NET.OpenGL;

namespace OwlRenderingSharp.Gpu.Tests;

[Collection(GlCollection.Name)]
public class GpuCubeTextureTests(GlFixture gl)
{
    [SkippableFact]
    public void EachFaceKeepsItsOwnColour()
    {
        gl.Require().Run(device =>
        {
            using GpuCubeTexture cube = new(device, "test cube", [0xFF0000FFu, 0xFF00FF00u, 0xFFFF0000u, 0xFF00FFFFu, 0xFFFF00FFu, 0xFFFFFF00u]);
            byte[] top = new byte[4];
            device.Gl.GetTextureSubImage(cube.Handle, 0, 0, 0, 2, 1, 1, 1, PixelFormat.Rgba, PixelType.UnsignedByte, 4, new Span<byte>(top));
            Assert.Equal([0, 0, 255, 255], top);
        });
    }

    [SkippableFact]
    public void ACubeNeedsSixFaces()
    {
        gl.Require().Run(device =>
        {
            Assert.Throws<GpuException>(() => new GpuCubeTexture(device, "short cube", [1u, 2u, 3u]));
        });
    }

    [SkippableFact]
    public void AColourTextureReadsBackAsFloatsWhateverItsStoredFormat()
    {
        gl.Require().Run(device =>
        {
            using GpuTexture texture = new(device, "bytes as floats", GpuFormat.Rgba8, 2, 1);
            texture.Upload<byte>([255, 0, 51, 255, 0, 255, 0, 255]);
            float[] pixels = texture.DownloadAsFloats();
            Assert.Equal(8, pixels.Length);
            Assert.Equal(1f, pixels[0], 1e-3f);
            Assert.Equal(0.2f, pixels[2], 1e-3f);
            Assert.Equal(1f, pixels[5], 1e-3f);
        });
    }
}
