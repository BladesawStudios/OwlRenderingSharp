using System.Numerics;
using OwlRenderingSharp.Content.Palettes;

namespace OwlRenderingSharp.Content.Tests;

public class EnvPaletteTests
{
    private static Dictionary<string, object?> Values(params (string Key, object? Value)[] items) => items.ToDictionary(i => i.Key, i => i.Value);

    private static Dictionary<string, object?> Color(double r, double g, double b) => new() { ["R"] = r, ["G"] = g, ["B"] = b, ["A"] = 0.5 };

    [Fact]
    public void ValuesAreReadWithFallbacksForMissingOrMistypedKeys()
    {
        EnvPalette palette = new("p", Values(("Exposure", 10.0), ("Flag", true), ("Text", "x"), ("Sun", Color(1, 0.5, 0.25))));
        Assert.Equal(10f, palette.GetFloat("Exposure", 0f));
        Assert.Equal(7f, palette.GetFloat("Missing", 7f));
        Assert.Equal(3f, palette.GetFloat("Text", 3f));
        Assert.True(palette.GetBool("Flag", false));
        Assert.Equal(new Vector4(1, 0.5f, 0.25f, 0.5f), palette.GetColor("Sun", Vector4.Zero));
    }

    [Fact]
    public void AColorMayBeAnArrayAndAGroupIsReadWithTheSameAccessors()
    {
        EnvPalette palette = new("p", Values(("Sky", new object?[] { 0.1, 0.2, 0.3 }), ("Fog", Values(("Density", 2.0)))));
        Assert.Equal(new Vector4(0.1f, 0.2f, 0.3f, 1f), palette.GetColor("Sky", Vector4.Zero));
        Assert.Equal(2f, palette.Group("Fog").GetFloat("Density", 0f));
        Assert.False(palette.Group("Nothing").Has("Density"));
    }

    [Fact]
    public void AChildPaletteOverridesItsParentKeyByKeyAndMergesNestedMaps()
    {
        Dictionary<string, IReadOnlyDictionary<string, object?>> raw = new()
        {
            ["Parent"] = Values(("A", 1.0), ("B", 2.0), ("Fog", Values(("Start", 1.0), ("End", 9.0)))),
            ["Child"] = Values(("$parent", "Work/WorldMgr/ResEnvPalette/Parent.game__wm__ResEnvPalette.gyml"), ("B", 5.0), ("Fog", Values(("End", 4.0)))),
        };
        EnvPalette child = new EnvPaletteLibrary(raw, []).Palette("Child");
        Assert.Equal(1f, child.GetFloat("A", 0f));
        Assert.Equal(5f, child.GetFloat("B", 0f));
        Assert.Equal(1f, child.Group("Fog").GetFloat("Start", 0f));
        Assert.Equal(4f, child.Group("Fog").GetFloat("End", 0f));
        Assert.False(child.Has("$parent"));
    }

    [Fact]
    public void AParentLoopDoesNotHang()
    {
        Dictionary<string, IReadOnlyDictionary<string, object?>> raw = new()
        {
            ["A"] = Values(("$parent", "B.gyml"), ("X", 1.0)),
            ["B"] = Values(("$parent", "A.gyml"), ("Y", 2.0)),
        };
        EnvPaletteLibrary library = new(raw, []);
        Assert.True(library.Palette("A").Has("X"));
    }

    [Fact]
    public void ANameMatchesWithoutRegardToCaseAndAnUnknownNameThrows()
    {
        EnvPaletteLibrary library = new(new Dictionary<string, IReadOnlyDictionary<string, object?>> { ["Noon"] = Values(("A", 1.0)) }, []);
        Assert.Equal("Noon", library.Palette("noon").Name);
        KeyNotFoundException error = Assert.Throws<KeyNotFoundException>(() => library.Palette("Dusk"));
        Assert.Contains("Dusk", error.Message);
    }
}
