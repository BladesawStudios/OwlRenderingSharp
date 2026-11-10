using OwlRenderingSharp.Content.Palettes;
using OwlRenderingSharp.Graph;

namespace OwlRenderingSharp.Games.Oot.Tests;

public class OotPostTests
{
    private static EnvPalette Palette(params (string, object)[] values) => new("test", values.ToDictionary(v => v.Item1, v => (object?)v.Item2));

    [Fact]
    public void ThePaletteGivesTheBloomItsThresholdInStopsAndItsOtherFields()
    {
        PostSettings post = OotPost.From(Palette(("BloomThreshold", -1.0), ("BloomIntensity", 0.15), ("BloomComposePower", 1.5), ("BloomClampedLuminance", 10.0)));
        Assert.Equal(0.5f, post.BloomThreshold, 1e-5f);
        Assert.Equal(0.15f, post.BloomIntensity, 1e-5f);
        Assert.Equal(1.5f, post.BloomPower, 1e-5f);
        Assert.Equal(10f, post.BloomClamp, 1e-5f);
    }

    [Fact]
    public void APaletteWithNoBloomFieldsHasNoBloom() => Assert.Equal(0f, OotPost.From(Palette()).BloomIntensity);

    [Fact]
    public void ADaylightTemperatureWithNoTintIsNeutralAndAWarmerOneCutsBlue()
    {
        Assert.Equal(System.Numerics.Vector3.One, OotPost.WhiteBalance(6500f, 0f));
        System.Numerics.Vector3 warm = OotPost.WhiteBalance(4500f, 0f);
        Assert.True(warm.Z > warm.X, "a lower white point is corrected toward blue");
    }

    [Fact]
    public void ATintScalesGreen() => Assert.Equal(1.05f, OotPost.WhiteBalance(6500f, -0.05f).Y, 1e-5f);
}
