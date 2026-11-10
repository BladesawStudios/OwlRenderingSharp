namespace OwlRenderingSharp.Gpu.Tests;

[Collection(GlCollection.Name)]
public class GpuSamplerCacheTests(GlFixture gl)
{
    [SkippableFact]
    public void EqualStatesShareOneSampler()
    {
        gl.Require().Run(device =>
        {
            using GpuSamplerCache cache = new(device);
            GpuSampler a = cache.Get(SamplerState.LinearClamp);
            GpuSampler b = cache.Get(SamplerState.LinearClamp with { });
            GpuSampler c = cache.Get(SamplerState.LinearClamp with { WrapU = TextureWrap.Repeat });
            Assert.Same(a, b);
            Assert.NotSame(a, c);
            Assert.Equal(2, cache.Count);
        });
    }
}
