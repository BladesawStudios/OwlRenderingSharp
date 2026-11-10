using System.Numerics;
using OwlRenderingSharp.Content.Palettes;

namespace OwlRenderingSharp.Games.Oot;

/// <summary>The lighting a frame is drawn under: a palette and where the main light stands.</summary>
/// <remarks>
/// The sun's azimuth and elevation are in degrees. <see cref="At"/> picks them and the palette from a place's time table the way the game does, the table's
/// main light when it forces one and otherwise a default high sun. <see cref="WithWhiteSky"/> replaces the hemisphere's top colour (the warm yellow of a Kokiri noon, which tints every surface) with white. <see cref="Neutral"/> is a palette-less environment whose lights are white and moderate, for
/// drawing a model with no place around it.
/// </remarks>
public sealed record OotEnvironment(EnvPalette Palette, float SunAzimuth, float SunElevation)
{
    public const float DefaultSunAzimuth = 0f, DefaultSunElevation = 50f;

    public static OotEnvironment Neutral { get; } = new(new EnvPalette("Neutral", new Dictionary<string, object?>()), DefaultSunAzimuth, DefaultSunElevation);

    public OotEnvironment WithWhiteSky() => this with { Palette = Palette.With("HemiLightColorTop", Vector4.One) };

    public static OotEnvironment At(EnvPaletteLibrary library, string timeTable, int hour, int minute)
    {
        EnvPaletteTimeTable table = library.TimeTable(timeTable);
        EnvPaletteTimeEntry entry = table.At(hour, minute) ?? throw new InvalidOperationException($"The time table {timeTable} has no entries");
        return new OotEnvironment(library.Palette(entry.Palette), table.MainLightAzimuth ?? DefaultSunAzimuth, table.MainLightElevation ?? DefaultSunElevation);
    }
}
