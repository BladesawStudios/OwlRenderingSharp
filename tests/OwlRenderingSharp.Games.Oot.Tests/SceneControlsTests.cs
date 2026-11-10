using System.Numerics;
using OwlRenderingSharp.Blocks;
using OwlRenderingSharp.Content.Palettes;
using OwlRenderingSharp.Graph;

namespace OwlRenderingSharp.Games.Oot.Tests;

public class SceneControlsTests
{
    private static EnvPalette Palette(params (string Key, object? Value)[] items) => new("p", items.ToDictionary(i => i.Key, i => i.Value));

    [Fact]
    public void TheSunScaleMultipliesTheSunColour()
    {
        BlockLayout layout = new OotBlockLayouts().Find(OotBlockLayouts.Environment)!;
        BlockWriter one = new(layout), two = new(layout);
        OotLights.Fill(one, OotEnvironment.Neutral);
        OotLights.Fill(two, OotEnvironment.Neutral, 2f);
        Assert.Equal(one.GetVector4("SunColor").X * 2f, two.GetVector4("SunColor").X, 1e-4f);
    }

    [Fact]
    public void FogDensityDividesTheDistanceFromStartToEndAndZeroTurnsFogOff()
    {
        EnvPalette palette = Palette(("FogStart", 10.0), ("FogEnd", 110.0));
        Assert.Equal(60f, OotAtmosphere.Fog(palette, 2f)!.End, 1e-3f);
        Assert.Equal(110f, OotAtmosphere.Fog(palette)!.End, 1e-3f);
        Assert.Null(OotAtmosphere.Fog(palette, 0f));
    }

    [Fact]
    public void WetnessDrivesTheAbsorptionFieldOfTheMaterialBlock()
    {
        Assert.Equal(0x324, WetnessControl.Absorption);
        Assert.Equal(1f, WetnessControl.Clamp(3f));
        Assert.Equal(0f, WetnessControl.Clamp(-1f));
        Assert.True(WetnessControl.MinimumBlockBytes > WetnessControl.Absorption);
    }
}
