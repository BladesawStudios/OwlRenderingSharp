using OwlRenderingSharp.Content.Palettes;

namespace OwlRenderingSharp.Content.Tests;

public class EnvPaletteLoaderTests
{
    [Fact]
    public void ARomfsWithoutThePackThrowsNamingIt()
    {
        string empty = Directory.CreateTempSubdirectory("owl-romfs").FullName;
        try
        {
            FileNotFoundException error = Assert.Throws<FileNotFoundException>(() => EnvPaletteLoader.Load(empty));
            Assert.Contains("EnvPalette.pack.zs", error.Message);
        }
        finally
        {
            Directory.Delete(empty, true);
        }
    }

    [SkippableFact]
    public void ARealRomfsYieldsPalettesAndTimeTables()
    {
        string? romfs = Environment.GetEnvironmentVariable("OWL_OOT_ROMFS");
        Skip.If(romfs is null || !Directory.Exists(romfs), "OWL_OOT_ROMFS does not name a romfs");
        EnvPaletteLibrary library = EnvPaletteLoader.Load(romfs!);
        Assert.True(library.PaletteNames.Count > 100);
        Assert.True(library.Palette("KokiriForest_Noon").GetFloat("BgDifIntensity", 0f) > 0f);
        Assert.NotEmpty(library.TimeTable("KokiriForest").Entries);
    }
}
