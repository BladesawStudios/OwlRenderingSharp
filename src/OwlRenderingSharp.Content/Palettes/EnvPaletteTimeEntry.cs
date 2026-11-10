namespace OwlRenderingSharp.Content.Palettes;

/// <summary>One row of a time table: from this hour and minute the named palette is in force, blended in over the given number of minutes.</summary>
/// <remarks>The blend time is in the game's own unit, taken as the table gives it.</remarks>
public sealed record EnvPaletteTimeEntry(string Name, int Hour, int Minute, string Palette, float BlendTime)
{
    public int Minutes => Hour * 60 + Minute;
}
