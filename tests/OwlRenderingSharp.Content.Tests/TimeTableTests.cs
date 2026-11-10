using OwlRenderingSharp.Content.Palettes;

namespace OwlRenderingSharp.Content.Tests;

public class TimeTableTests
{
    private static Dictionary<string, object?> Row(string name, double hour, double min, string palette) => new()
    {
        ["Name"] = name,
        ["Hour"] = hour,
        ["Min"] = min,
        ["BlendTime"] = 15.0,
        ["EnvPalette"] = $"Work/WorldMgr/ResEnvPalette/{palette}.game__wm__ResEnvPalette.gyml",
    };

    private static EnvPaletteTimeTable Table() => TimeTableParser.Parse("KokiriForest", new Dictionary<string, object?>
    {
        ["MainLight_Azimuth"] = 333.5,
        ["MainLight_Elevation"] = 50.5,
        ["TimeTableArray"] = new object?[] { Row("Noon", 5, 30, "KokiriForest_Noon"), Row("Night", 19, 30, "KokiriForest_Night") },
    });

    [Fact]
    public void RowsAndTheMainLightAreParsed()
    {
        EnvPaletteTimeTable table = Table();
        Assert.Equal(2, table.Entries.Count);
        Assert.Equal("KokiriForest_Noon", table.Entries[0].Palette);
        Assert.Equal(330, table.Entries[0].Minutes);
        Assert.Equal(333.5f, table.MainLightAzimuth);
        Assert.Equal(50.5f, table.MainLightElevation);
    }

    [Theory]
    [InlineData(6, 0, "Noon")]
    [InlineData(19, 30, "Night")]
    [InlineData(23, 0, "Night")]
    [InlineData(2, 0, "Night")]
    public void TheEntryInForceIsTheLatestThatHasStarted(int hour, int minute, string expected)
    {
        Assert.Equal(expected, Table().At(hour, minute)!.Name);
    }

    [Fact]
    public void ATableWithoutRowsHasNoEntryAndNoMainLight()
    {
        EnvPaletteTimeTable table = TimeTableParser.Parse("Empty", new Dictionary<string, object?>());
        Assert.Null(table.At(12, 0));
        Assert.Null(table.MainLightAzimuth);
    }
}
