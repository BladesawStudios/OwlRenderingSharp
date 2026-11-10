using System.Numerics;

namespace OwlRenderingSharp.Graph.Tests;

public class SkyProbeTests
{
    private static readonly Vector3 Sky = new(3f, 2f, 1f), Ground = new(0.1f, 0.1f, 0.1f);

    [Fact]
    public void LookingStraightUpSeesTheSkyAndStraightDownTheGround()
    {
        Assert.Equal(Sky, SkyProbe.Blend(Sky, Ground, Vector3.UnitY));
        Assert.Equal(Ground, SkyProbe.Blend(Sky, Ground, -Vector3.UnitY));
    }

    [Fact]
    public void TheHorizonIsAnEvenMix()
    {
        Vector3 horizon = SkyProbe.Blend(Sky, Ground, Vector3.UnitX);
        Assert.Equal((Sky + Ground) * 0.5f, horizon);
    }

    [Fact]
    public void TheFacesHaveSixTimesTheTexelsAndKeepLightBrighterThanOne()
    {
        float[][] faces = SkyProbe.Faces(Sky, Ground, 8);
        Assert.Equal(6, faces.Length);
        Assert.All(faces, face => Assert.Equal(8 * 8 * 4, face.Length));
        Assert.True(faces[2].Where((_, i) => i % 4 == 0).Max() > 2.5f, "the +Y face looks up at the sky");
        Assert.True(faces[3].Where((_, i) => i % 4 == 0).Max() < 0.5f, "the -Y face looks down at the ground");
    }

    [Fact]
    public void TheTopRowOfASideFaceIsSkierThanItsBottomRow()
    {
        float[] side = SkyProbe.Faces(Sky, Ground, 8)[0];
        Assert.True(side[0] > side[(7 * 8) * 4]);
    }

    [Fact]
    public void TheMipChainRunsDownToOneTexel() => Assert.Equal(6, SkyProbe.MipLevels);

    [Fact]
    public void ASunLobeBrightensOnlyTheDirectionOfTheSun()
    {
        SkySun sun = new(Vector3.UnitX, Vector3.One * 10f);
        Assert.Equal(10f, sun.At(Vector3.UnitX).X, 0.01f);
        Assert.Equal(0f, sun.At(-Vector3.UnitX).X, 0.01f);
    }
}
