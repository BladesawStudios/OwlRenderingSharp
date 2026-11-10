using OwlRenderingSharp.Gpu;

namespace OwlRenderingSharp.Graph.Tests;

public class FrameSamplerDefaultsTests
{
    [Fact]
    public void ABufferThatIsNotThereReadsAsNothingInIt()
    {
        Assert.Equal(0x00000000u, FrameSamplerDefaults.For("gsys_projection0"));
        Assert.Equal(0xFF000000u, FrameSamplerDefaults.For("gsys_gbuffer_albedo"));
        Assert.Equal(0xFFFFFFFFu, FrameSamplerDefaults.For("gsys_shadow_prepass"));
    }

    [Fact]
    public void TheReflectionCubeIsDimNotWhite()
    {
        uint cube = FrameSamplerDefaults.For("gsys_cube_map");
        Assert.NotEqual(0xFFFFFFFFu, cube);
        Assert.Equal(0xFFu, cube >> 24);
    }

    [Fact]
    public void AnUnlistedSamplerIsWhite()
    {
        Assert.Equal(0xFFFFFFFFu, FrameSamplerDefaults.For("_a0"));
    }

    [Fact]
    public void AnEmptyVolumeTextureIsDarkSoNoProbeLightIsInvented()
    {
        Assert.Equal(0xFF000000u, FrameSamplerDefaults.For("gsys_user3", GpuSamplerKind.Texture3D));
        Assert.Equal(0xFFFFFFFFu, FrameSamplerDefaults.For("gsys_user3", GpuSamplerKind.Texture2D));
        Assert.Equal(FrameSamplerDefaults.For("gsys_cube_map"), FrameSamplerDefaults.For("gsys_cube_map", GpuSamplerKind.Texture3D));
    }
}
