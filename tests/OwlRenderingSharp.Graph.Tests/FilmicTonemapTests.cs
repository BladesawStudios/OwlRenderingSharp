namespace OwlRenderingSharp.Graph.Tests;

public class FilmicTonemapTests
{
    [Fact]
    public void BlackStaysBlackAndAnyBrightnessStaysInRange()
    {
        Assert.Equal(0, FilmicTonemap.Encode(0f, 1f));
        Assert.Equal(255, FilmicTonemap.Encode(1000f, 1f));
        Assert.Equal(0, FilmicTonemap.Encode(-3f, 1f));
    }

    [Fact]
    public void TheCurveRisesAndCompressesTheHighlights()
    {
        float[] samples = [0.1f, 0.5f, 1f, 2f, 8f];
        Assert.True(samples.Zip(samples.Skip(1), (a, b) => FilmicTonemap.Curve(a) < FilmicTonemap.Curve(b)).All(ok => ok));
        Assert.True(FilmicTonemap.Curve(8f) - FilmicTonemap.Curve(2f) < FilmicTonemap.Curve(1f) - FilmicTonemap.Curve(0.5f));
    }

    [Fact]
    public void TheExposureScalesTheLightBeforeTheCurve()
    {
        Assert.Equal(FilmicTonemap.Encode(0.5f, 1f), FilmicTonemap.Encode(1f, 0.5f));
    }

    [Fact]
    public void AlphaIsKeptAsItIsAndColourIsEncodedPerChannel()
    {
        byte[] pixels = FilmicTonemap.ToBytes([0f, 1f, 100f, 0.5f], 1f);
        Assert.Equal(0, pixels[0]);
        Assert.Equal(255, pixels[2]);
        Assert.Equal(128, pixels[3]);
    }
}
