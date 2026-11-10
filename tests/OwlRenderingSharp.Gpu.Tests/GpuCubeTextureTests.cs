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
    public void AFloatCubeKeepsLightAboveOneAndBuildsItsMips()
    {
        gl.Require().Run(device =>
        {
            float[] face = Enumerable.Range(0, 4 * 4).SelectMany(_ => new[] { 3f, 2f, 1f, 1f }).ToArray();
            using GpuCubeTexture cube = new(device, "float cube", 4, Enumerable.Repeat(face, 6).ToArray());
            Assert.Equal(3, cube.Levels);
            float[] last = new float[4];
            device.Gl.GetTextureSubImage(cube.Handle, 2, 0, 0, 0, 1, 1, 1, PixelFormat.Rgba, PixelType.Float, 16, new Span<float>(last));
            Assert.Equal(3f, last[0], 0.01f);
            Assert.Equal(2f, last[1], 0.01f);
        });
    }

    [SkippableFact]
    public void AFloatCubeNeedsFacesOfTheRightSize()
    {
        gl.Require().Run(device => Assert.Throws<GpuException>(() => new GpuCubeTexture(device, "bad cube", 4, Enumerable.Repeat(new float[8], 6).ToArray())));
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

    [SkippableFact]
    public void ASwizzleSetsWhatEachSampledChannelReads()
    {
        gl.Require().Run(device =>
        {
            using GpuTexture texture = new(device, "one channel", GpuFormat.R8, 1, 1);
            texture.Swizzle(GpuChannel.Red, GpuChannel.Red, GpuChannel.Red, GpuChannel.One);
            device.Gl.GetTextureParameter(texture.Handle, GLEnum.TextureSwizzleG, out int green);
            device.Gl.GetTextureParameter(texture.Handle, GLEnum.TextureSwizzleB, out int blue);
            device.Gl.GetTextureParameter(texture.Handle, GLEnum.TextureSwizzleA, out int alpha);
            Assert.Equal((int)GLEnum.Red, green);
            Assert.Equal((int)GLEnum.Red, blue);
            Assert.Equal((int)GLEnum.One, alpha);
        });
    }
}
