namespace OwlRenderingSharp.Gpu.Tests;

[Collection(GlCollection.Name)]
public class GpuTextureTests(GlFixture gl)
{
    [SkippableFact]
    public void PixelsComeBackAsUploaded()
    {
        byte[] pixels = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16];
        byte[] back = gl.Require().Run(device =>
        {
            using GpuTexture texture = new(device, "t", GpuFormat.Rgba8, 2, 2);
            texture.Upload<byte>(pixels);
            return texture.Download<byte>();
        });
        Assert.Equal(pixels, back);
    }

    [SkippableFact]
    public void AMipLevelHasHalfTheSize()
    {
        float[] back = gl.Require().Run(device =>
        {
            using GpuTexture texture = new(device, "t", GpuFormat.R32F, 4, 4, mipCount: 3);
            texture.Upload<float>(new float[1], level: 2);
            return texture.Download<float>(level: 2);
        });
        Assert.Single(back);
    }

    [SkippableFact]
    public void AnUploadOfTheWrongSizeNamesTheTexture()
    {
        GpuException error = Assert.Throws<GpuException>(() => gl.Require().Run(device =>
        {
            using GpuTexture texture = new(device, "albedo", GpuFormat.Rgba8, 2, 2);
            texture.Upload<byte>(new byte[3]);
        }));
        Assert.Equal("albedo", error.Resource);
    }

    [SkippableFact]
    public void AMissingLevelIsAnError()
    {
        Assert.Throws<GpuException>(() => gl.Require().Run(device =>
        {
            using GpuTexture texture = new(device, "t", GpuFormat.Rgba8, 2, 2);
            texture.Download<byte>(level: 1);
        }));
    }

    [SkippableFact]
    public void ASamplerCanBeCreatedWithEveryFilterAndWrap()
    {
        gl.Require().Run(device =>
        {
            foreach (TextureFilter filter in Enum.GetValues<TextureFilter>())
            foreach (TextureWrap wrap in Enum.GetValues<TextureWrap>())
            {
                using GpuSampler sampler = new(device, "s", new SamplerState(filter, TextureFilter.Linear, wrap, wrap, Anisotropy: 4, Compare: wrap == TextureWrap.Clamp));
                Assert.NotEqual(0u, sampler.Handle);
            }
        });
    }
}
