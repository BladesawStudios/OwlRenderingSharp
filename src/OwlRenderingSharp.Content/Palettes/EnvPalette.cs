using System.Globalization;
using System.Numerics;

namespace OwlRenderingSharp.Content.Palettes;

/// <summary>One environment palette: the named values (lights, fog, exposure, grading) the game picks for a place and a time of day.</summary>
/// <remarks>
/// The values are the palette's data file read into plain objects: numbers as double, text as string, booleans, arrays, and maps as dictionaries. A colour is a
/// map with R, G, B and optionally A, or an array of three or four numbers. A read of a missing or mistyped key returns the fallback the caller gives, so a palette
/// that omits a field behaves as the caller's default.
/// </remarks>
public sealed class EnvPalette(string name, IReadOnlyDictionary<string, object?> values)
{
    public string Name { get; } = name;

    public IReadOnlyCollection<string> Keys => values.Keys.ToArray();

    public bool Has(string key) => values.ContainsKey(key);

    public float GetFloat(string key, float fallback) => values.TryGetValue(key, out object? raw) && TryFloat(raw, out float value) ? value : fallback;

    public bool GetBool(string key, bool fallback) => values.TryGetValue(key, out object? raw) && raw is bool value ? value : fallback;

    public Vector4 GetColor(string key, Vector4 fallback) => values.TryGetValue(key, out object? raw) && TryColor(raw, out Vector4 color) ? color : fallback;

    // A nested group of values, such as one volume fog layer, read with the same accessors; an absent or non-map key gives an empty palette.
    public EnvPalette Group(string key) =>
        new($"{Name}/{key}", values.TryGetValue(key, out object? raw) && raw is IReadOnlyDictionary<string, object?> map ? map : new Dictionary<string, object?>());

    private static bool TryFloat(object? raw, out float value)
    {
        switch (raw)
        {
            case double d:
                value = (float)d;
                return true;
            case string s when float.TryParse(s, CultureInfo.InvariantCulture, out float parsed):
                value = parsed;
                return true;
            default:
                value = 0f;
                return false;
        }
    }

    private static float Component(object? raw) => TryFloat(raw, out float value) ? value : 0f;

    private static bool TryColor(object? raw, out Vector4 color)
    {
        color = default;
        if (raw is object?[] { Length: >= 3 } array)
        {
            color = new Vector4(Component(array[0]), Component(array[1]), Component(array[2]), array.Length >= 4 ? Component(array[3]) : 1f);
            return true;
        }

        if (raw is IReadOnlyDictionary<string, object?> map && map.ContainsKey("R"))
        {
            color = new Vector4(Component(map["R"]), Component(map.GetValueOrDefault("G")), Component(map.GetValueOrDefault("B")), map.TryGetValue("A", out object? a) ? Component(a) : 1f);
            return true;
        }

        return false;
    }
}
