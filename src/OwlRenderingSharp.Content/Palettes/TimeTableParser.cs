namespace OwlRenderingSharp.Content.Palettes;

/// <summary>Reads a time table from the plain values of its data file.</summary>
/// <remarks>
/// The file holds <c>MainLight_Azimuth</c> and <c>MainLight_Elevation</c> and a <c>TimeTableArray</c> of rows with <c>Name</c>, <c>Hour</c>, <c>Min</c>, <c>BlendTime</c> and
/// an <c>EnvPalette</c> path whose file name before the first dot is the palette's name. A row without a palette is left out.
/// </remarks>
public static class TimeTableParser
{
    public static EnvPaletteTimeTable Parse(string name, IReadOnlyDictionary<string, object?> values)
    {
        List<EnvPaletteTimeEntry> entries = [];
        if (values.GetValueOrDefault("TimeTableArray") is object?[] rows)
        {
            foreach (IReadOnlyDictionary<string, object?> row in rows.OfType<IReadOnlyDictionary<string, object?>>())
            {
                if (row.GetValueOrDefault("EnvPalette") is not string path) continue;
                string file = Path.GetFileName(path.Replace('\\', '/'));
                int dot = file.IndexOf('.');
                entries.Add(new EnvPaletteTimeEntry(
                    row.GetValueOrDefault("Name") as string ?? "",
                    (int)Number(row, "Hour"),
                    (int)Number(row, "Min"),
                    dot < 0 ? file : file[..dot],
                    Number(row, "BlendTime")));
            }
        }

        return new EnvPaletteTimeTable(name, entries, Optional(values, "MainLight_Azimuth"), Optional(values, "MainLight_Elevation"));
    }

    private static float Number(IReadOnlyDictionary<string, object?> map, string key) => map.GetValueOrDefault(key) is double d ? (float)d : 0f;

    private static float? Optional(IReadOnlyDictionary<string, object?> map, string key) => map.GetValueOrDefault(key) is double d ? (float)d : null;
}
