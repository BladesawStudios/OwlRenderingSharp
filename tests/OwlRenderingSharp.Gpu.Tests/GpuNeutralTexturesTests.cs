namespace OwlRenderingSharp.Gpu.Tests;

[Collection(GlCollection.Name)]
public class GpuNeutralTexturesTests(GlFixture gl)
{
    [SkippableFact]
    public void EveryKindHasATextureAndTheSameOneComesBack()
    {
        gl.Require().Run(device =>
        {
            using GpuNeutralTextures neutral = new(device);
            foreach (GpuSamplerKind kind in Enum.GetValues<GpuSamplerKind>())
                Assert.NotEqual(0u, neutral.Handle(kind));
            Assert.Equal(neutral.Handle(GpuSamplerKind.TextureCube), neutral.Handle(GpuSamplerKind.TextureCube));
            Assert.NotEqual(neutral.Handle(GpuSamplerKind.Texture2D), neutral.Handle(GpuSamplerKind.Texture3D));
        });
    }
}
