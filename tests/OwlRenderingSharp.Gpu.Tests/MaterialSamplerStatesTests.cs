using BfShaderSharp.Pack.Records;

namespace OwlRenderingSharp.Gpu.Tests;

public class MaterialSamplerStatesTests
{
    private static SamplerRecord Record(byte wrapU = 0, byte wrapV = 0, ushort filter = 0, byte anisotropy = 1, byte compare = 0) =>
        new("s", "t", wrapU, wrapV, 2, compare, 0, anisotropy, filter, 0, 13, 0.5f);

    [Fact]
    public void LinearEverywhereIsTrilinear()
    {
        SamplerState state = MaterialSamplerStates.From(Record(filter: 0b101010));
        Assert.Equal((TextureFilter.LinearMipLinear, TextureFilter.Linear), (state.Min, state.Mag));
    }

    [Fact]
    public void PointsMinificationWithPointMipsIsNearestMipNearest()
    {
        SamplerState state = MaterialSamplerStates.From(Record(filter: 0b010101));
        Assert.Equal((TextureFilter.NearestMipNearest, TextureFilter.Nearest), (state.Min, state.Mag));
    }

    [Fact]
    public void NoMipFilterMeansNoMips()
    {
        Assert.Equal(TextureFilter.Linear, MaterialSamplerStates.From(Record(filter: 0b100000)).Min);
        Assert.Equal(TextureFilter.Nearest, MaterialSamplerStates.From(Record(filter: 0b010000)).Min);
    }

    [Fact]
    public void WrapModesMapAndUnknownOnesClamp()
    {
        SamplerState state = MaterialSamplerStates.From(Record(wrapU: 1, wrapV: 4));
        Assert.Equal((TextureWrap.Mirror, TextureWrap.MirrorClamp), (state.WrapU, state.WrapV));
        Assert.Equal(TextureWrap.Clamp, MaterialSamplerStates.From(Record(wrapU: 3)).WrapU);
        Assert.Equal(TextureWrap.Repeat, MaterialSamplerStates.From(Record(wrapU: 0)).WrapU);
    }

    [Fact]
    public void AnisotropyLodAndCompareAreCarried()
    {
        SamplerState state = MaterialSamplerStates.From(Record(anisotropy: 16, compare: 3));
        Assert.Equal((16f, 13f, 0.5f, true), (state.Anisotropy, state.MaxLod, state.LodBias, state.Compare));
        Assert.Equal(1f, MaterialSamplerStates.From(Record(anisotropy: 0)).Anisotropy);
    }
}
