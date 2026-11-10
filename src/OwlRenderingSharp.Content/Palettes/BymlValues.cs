using BymlLibrary;

namespace OwlRenderingSharp.Content.Palettes;

/// <summary>Reads a BYML node into the plain objects <see cref="EnvPalette"/> holds.</summary>
/// <remarks>Every number becomes a double, whatever its stored width; maps keep their keys, arrays become object arrays, and binary or null nodes become null.</remarks>
public static class BymlValues
{
    public static object? From(Byml node) => node.Type switch
    {
        BymlNodeType.String => node.GetString(),
        BymlNodeType.Bool => node.GetBool(),
        BymlNodeType.Int => (double)node.GetInt(),
        BymlNodeType.UInt32 => (double)node.GetUInt32(),
        BymlNodeType.Int64 => (double)node.GetInt64(),
        BymlNodeType.UInt64 => (double)node.GetUInt64(),
        BymlNodeType.Float => (double)node.GetFloat(),
        BymlNodeType.Double => node.GetDouble(),
        BymlNodeType.Array => node.GetArray().Select(From).ToArray(),
        BymlNodeType.Map => node.GetMap().ToDictionary(pair => pair.Key, pair => From(pair.Value), StringComparer.Ordinal),
        _ => null,
    };

    public static IReadOnlyDictionary<string, object?> Map(Byml node) =>
        From(node) as IReadOnlyDictionary<string, object?> ?? new Dictionary<string, object?>();
}
