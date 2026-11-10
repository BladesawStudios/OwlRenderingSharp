using System.Numerics;
using OwlRenderingSharp.Blocks;
using OwlRenderingSharp.Content.Palettes;

namespace OwlRenderingSharp.Games.Oot.Tests;

public class OotLightsTests
{
    private static BlockWriter Fill(OotEnvironment environment)
    {
        BlockWriter writer = new(new OotBlockLayouts().Find(OotBlockLayouts.Environment)!);
        OotLights.Fill(writer, environment);
        return writer;
    }

    private static EnvPalette Palette(params (string Key, object? Value)[] items) => new("p", items.ToDictionary(i => i.Key, i => i.Value));

    [Fact]
    public void TheSunIsItsColorTimesIntensityOverTwoToTheExposure()
    {
        EnvPalette palette = Palette(("ExposureValue", 10.0), ("BgDifIntensity", 10240.0), ("BgDifColor", new Dictionary<string, object?> { ["R"] = 1.0, ["G"] = 0.5, ["B"] = 0.0 }));
        Vector4 color = Fill(new OotEnvironment(palette, 0, 45)).GetVector4("SunColor");
        Assert.Equal(10f, color.X, 1e-4f);
        Assert.Equal(5f, color.Y, 1e-4f);
        Assert.Equal(0f, color.Z, 1e-4f);
    }

    [Fact]
    public void TheHemisphereIsItsColorsTimesItsIntensity()
    {
        EnvPalette palette = Palette(("HemiLightIntensity", 2.0), ("HemiLightColorTop", new object?[] { 0.5, 0.25, 1.0 }));
        Vector4 sky = Fill(new OotEnvironment(palette, 0, 45)).GetVector4("HemiSky");
        Assert.Equal(new Vector4(1f, 0.5f, 2f, 1f), sky);
    }

    [Fact]
    public void TheMainLightTravelsAwayFromTheSunInWorldSpace()
    {
        Vector3 toSun = OotLights.ToSun(new OotEnvironment(Palette(), 90, 0));
        Assert.Equal(1f, toSun.X, 1e-5f);
        Vector4 travel = Fill(new OotEnvironment(Palette(), 90, 0)).GetVector4("LightDirectionWorld");
        Assert.Equal(-1f, travel.X, 1e-5f);
    }

    [Fact]
    public void ThePointLightsAreInertAndFarAway()
    {
        BlockWriter writer = Fill(OotEnvironment.Neutral);
        Assert.Equal(Vector4.Zero, writer.GetVector4("PointLightColor0"));
        Assert.Equal(1e6f, writer.GetVector4("PointLightPosition1").Y);
    }

    [Fact]
    public void TheEnvironmentTakesItsPaletteAndSunFromTheTimeTable()
    {
        Dictionary<string, IReadOnlyDictionary<string, object?>> palettes = new() { ["Noon"] = new Dictionary<string, object?>(), ["Night"] = new Dictionary<string, object?>() };
        EnvPaletteTimeTable table = new("Place", [new EnvPaletteTimeEntry("Noon", 6, 0, "Noon", 0), new EnvPaletteTimeEntry("Night", 19, 0, "Night", 0)], 120f, 30f);
        OotEnvironment environment = OotEnvironment.At(new EnvPaletteLibrary(palettes, [table]), "Place", 20, 0);
        Assert.Equal("Night", environment.Palette.Name);
        Assert.Equal((120f, 30f), (environment.SunAzimuth, environment.SunElevation));
    }
}
