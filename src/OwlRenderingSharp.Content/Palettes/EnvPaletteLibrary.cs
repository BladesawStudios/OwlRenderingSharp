namespace OwlRenderingSharp.Content.Palettes;

/// <summary>Every palette and time table of a game, found by name.</summary>
/// <remarks>
/// A palette may name another in <c>$parent</c> and override only some of its values; the library flattens each such chain when it is built, merging maps key by key,
/// and a chain that loops stops where it closes. A parent is named by a path whose file name before the first dot is the palette's name. A name the library does
/// not hold throws <see cref="KeyNotFoundException"/> naming it; names match without regard to case when no exact match exists.
/// </remarks>
public sealed class EnvPaletteLibrary
{
    private readonly Dictionary<string, EnvPalette> _palettes = new(StringComparer.Ordinal);
    private readonly Dictionary<string, EnvPaletteTimeTable> _tables = new(StringComparer.Ordinal);

    public EnvPaletteLibrary(IReadOnlyDictionary<string, IReadOnlyDictionary<string, object?>> palettes, IEnumerable<EnvPaletteTimeTable> tables)
    {
        Dictionary<string, IReadOnlyDictionary<string, object?>> resolved = new(StringComparer.Ordinal);
        foreach (string name in palettes.Keys) _palettes[name] = new EnvPalette(name, Resolve(name, palettes, resolved, []));
        foreach (EnvPaletteTimeTable table in tables) _tables[table.Name] = table;
    }

    public IReadOnlyCollection<string> PaletteNames => _palettes.Keys;

    public IReadOnlyCollection<string> TimeTableNames => _tables.Keys;

    public EnvPalette Palette(string name) => Find(_palettes, name, "palette");

    public EnvPaletteTimeTable TimeTable(string name) => Find(_tables, name, "time table");

    private static T Find<T>(Dictionary<string, T> items, string name, string kind)
    {
        if (items.TryGetValue(name, out T? exact)) return exact;
        foreach ((string key, T item) in items)
            if (string.Equals(key, name, StringComparison.OrdinalIgnoreCase)) return item;
        throw new KeyNotFoundException($"The game has no {kind} named {name}");
    }

    private static IReadOnlyDictionary<string, object?> Resolve(
        string name, IReadOnlyDictionary<string, IReadOnlyDictionary<string, object?>> raw, Dictionary<string, IReadOnlyDictionary<string, object?>> done, HashSet<string> visiting)
    {
        if (done.TryGetValue(name, out IReadOnlyDictionary<string, object?>? known)) return known;
        if (!raw.TryGetValue(name, out IReadOnlyDictionary<string, object?>? own) || !visiting.Add(name)) return own ?? new Dictionary<string, object?>();
        IReadOnlyDictionary<string, object?> merged = own;
        if (own.TryGetValue("$parent", out object? parent) && parent is string path)
            merged = Merge(Resolve(ParentName(path), raw, done, visiting), own);
        return done[name] = merged;
    }

    private static string ParentName(string path)
    {
        string file = Path.GetFileName(path.Replace('\\', '/'));
        int dot = file.IndexOf('.');
        return dot < 0 ? file : file[..dot];
    }

    private static Dictionary<string, object?> Merge(IReadOnlyDictionary<string, object?> parent, IReadOnlyDictionary<string, object?> child)
    {
        Dictionary<string, object?> merged = new(parent, StringComparer.Ordinal);
        foreach ((string key, object? value) in child)
        {
            if (key == "$parent") continue;
            merged[key] = value is IReadOnlyDictionary<string, object?> childMap && merged.GetValueOrDefault(key) is IReadOnlyDictionary<string, object?> parentMap
                ? Merge(parentMap, childMap)
                : value;
        }

        return merged;
    }
}
