using BymlLibrary;
using SarcLibrary;
using ZsDicSharp;

namespace OwlRenderingSharp.Content.Palettes;

/// <summary>Reads the palette library of a game from its romfs.</summary>
/// <remarks>
/// The palettes are in <c>Pack/EnvPalette.pack.zs</c>, an archive of BYML files: <c>WorldMgr/ResEnvPalette</c> holds the palettes and
/// <c>WorldMgr/ResEnvPaletteTimeTable</c> the time tables, each named by its file name before the first dot. A romfs without the pack, or a pack that is not an archive,
/// throws <see cref="FileNotFoundException"/> or <see cref="InvalidDataException"/> naming the path.
/// </remarks>
public static class EnvPaletteLoader
{
    public const string PackPath = "Pack/EnvPalette.pack.zs";
    private const string PaletteDirectory = "WorldMgr/ResEnvPalette/", TimeTableDirectory = "WorldMgr/ResEnvPaletteTimeTable/";

    public static EnvPaletteLibrary Load(string romfs)
    {
        string path = Path.Combine(romfs, PackPath);
        if (!File.Exists(path)) throw new FileNotFoundException($"The romfs has no {PackPath}", path);
        using ZsDic zstd = ZsDic.FromRomfs(romfs);
        Sarc archive;
        try
        {
            archive = Sarc.FromBinary(zstd.DecompressFile(path));
        }
        catch (Exception e) when (e is not FileNotFoundException)
        {
            throw new InvalidDataException($"{path} is not a readable archive: {e.Message}", e);
        }

        Dictionary<string, IReadOnlyDictionary<string, object?>> palettes = new(StringComparer.Ordinal);
        List<EnvPaletteTimeTable> tables = [];
        foreach (string key in archive.Keys)
        {
            if (key.StartsWith(PaletteDirectory, StringComparison.Ordinal)) palettes[Name(key)] = Read(archive, key);
            else if (key.StartsWith(TimeTableDirectory, StringComparison.Ordinal)) tables.Add(TimeTableParser.Parse(Name(key), Read(archive, key)));
        }

        return new EnvPaletteLibrary(palettes, tables);
    }

    private static IReadOnlyDictionary<string, object?> Read(Sarc archive, string key) => BymlValues.Map(Byml.FromBinary(archive[key].ToArray()));

    private static string Name(string key)
    {
        string file = key[(key.LastIndexOf('/') + 1)..];
        int dot = file.IndexOf('.');
        return dot < 0 ? file : file[..dot];
    }
}
