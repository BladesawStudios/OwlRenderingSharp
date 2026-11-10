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
    public void TheHemisphereIsItsColorsTimesItsIntensityAndTheScale()
    {
        EnvPalette palette = Palette(("HemiLightIntensity", 2.0), ("HemiLightColorTop", new object?[] { 0.5, 0.25, 1.0 }));
        Vector4 sky = Fill(new OotEnvironment(palette, 0, 45)).GetVector4("HemiSky");
        Assert.Equal(new Vector4(new Vector3(1f, 0.5f, 2f) * OotLights.HemiScale, 1f), sky);
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
    public void TheCharacterRigIsAKeyAboveLeftAFillBelowRightAndARimBehindEachAShareOfTheSun()
    {
        BlockWriter writer = Fill(OotEnvironment.Neutral);
        Vector4 sun = writer.GetVector4("SunColor");
        Assert.Equal(sun.X * OotLights.KeyScale, writer.GetVector4("PointLightColor0").X, 1e-4f);
        Assert.Equal(sun.X * OotLights.FillScale, writer.GetVector4("PointLightColor1").X, 1e-4f);
        Assert.Equal(sun.X * OotLights.RimScale, writer.GetVector4("PointLightColor2").X, 1e-4f);
        Vector4 key = writer.GetVector4("PointLightPosition0"), fill = writer.GetVector4("PointLightPosition1"), rim = writer.GetVector4("PointLightPosition2");
        Assert.True(key.X < 0f && key.Y > 0f);
        Assert.True(fill.X > 0f && fill.Y < key.Y);
        Assert.True(rim.Z < key.Z);
    }

    [Fact]
    public void AWhiteSkyReplacesTheHemisphereTopColourAndNothingElse()
    {
        EnvPalette palette = Palette(("HemiLightColorTop", new object?[] { 1.0, 0.5, 0.2, 1.0 }), ("FogStart", 7.0));
        EnvPalette white = new OotEnvironment(palette, 0, 45).WithWhiteSky().Palette;
        Assert.Equal(Vector4.One, white.GetColor("HemiLightColorTop", Vector4.Zero));
        Assert.Equal(7f, white.GetFloat("FogStart", 0f));
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

    [Fact]
    public void TheSkyColoursAreTheHemisphereLightsAndFeedTheEnvironmentBlock()
    {
        (System.Numerics.Vector3 sky, System.Numerics.Vector3 ground) = OotLights.SkyColors(OotEnvironment.Neutral);
        Assert.Equal(System.Numerics.Vector3.One * OotLights.HemiScale, sky);
        Assert.Equal(new System.Numerics.Vector3(0.5f) * OotLights.HemiScale, ground);
        BlockWriter writer = new(new OotBlockLayouts().Find("gsys_environment")!);
        OotLights.Fill(writer, OotEnvironment.Neutral);
        Assert.Equal(new System.Numerics.Vector4(sky, 1f), writer.GetVector4("HemiSky"));
    }
}
