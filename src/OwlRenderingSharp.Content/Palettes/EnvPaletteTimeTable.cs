namespace OwlRenderingSharp.Content.Palettes;

/// <summary>When, in the hours of a day, each palette of a place takes over, and where the main light stands for the place.</summary>
/// <remarks>
/// An entry applies from its hour and minute until the next entry's. Before the first entry of the day the last entry still applies, since the day wraps. The main
/// light's azimuth and elevation are in degrees, as the game stores them; a table that gives none has no forced main light and both are null.
/// </remarks>
public sealed class EnvPaletteTimeTable(string name, IReadOnlyList<EnvPaletteTimeEntry> entries, float? mainLightAzimuth, float? mainLightElevation)
{
    public string Name { get; } = name;

    public IReadOnlyList<EnvPaletteTimeEntry> Entries { get; } = entries;

    public float? MainLightAzimuth { get; } = mainLightAzimuth;

    public float? MainLightElevation { get; } = mainLightElevation;

    // The entry in force at the given time of day, or null when the table has no entries.
    public EnvPaletteTimeEntry? At(int hour, int minute)
    {
        if (Entries.Count == 0) return null;
        int now = hour * 60 + minute;
        List<EnvPaletteTimeEntry> ordered = [.. Entries.OrderBy(e => e.Minutes)];
        return ordered.LastOrDefault(e => e.Minutes <= now) ?? ordered[^1];
    }
}
