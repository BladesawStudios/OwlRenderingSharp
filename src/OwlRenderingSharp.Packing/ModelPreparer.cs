using BfShaderSharp.Assets;
using BfShaderSharp.Bfsha;
using BfShaderSharp.Pack.Records;
using BfShaderSharp.Preparation;
using OwlRenderingSharp.Content;
using OwlRenderingSharp.Graph;
using Yaz0Sharp;
using ZsDicSharp;

namespace OwlRenderingSharp.Packing;

/// <summary>What was prepared for one model: its asset pack, its prepared shader archive and its name inside the pack.</summary>
public sealed record PreparedModel(string Pack, string Shaders, string Name);

/// <summary>Prepares one model of the game's romfs into a cache folder.</summary>
/// <remarks>
/// A model is prepared on its own: its materials pick their shader programs, and only those programs are decompiled into a prepared shader archive for it, which takes seconds where the whole
/// game's archive takes minutes. Both files are named by the hash of the model file, so reopening an unchanged file reuses them. Archives prepared for the same set of programs are shared between
/// models, and the archive read last stays in memory while the process lives.
/// </remarks>
public static class ModelPreparer
{
    public static PreparedModel Prepare(string romfs, string cache, string modelPath, IReadOnlyList<string> romfsPaths, byte[]? edited, IProgress<string>? stage)
    {
        stage?.Report("Reading the model");
        // An edited model is prepared from its bytes as they are now (plain BFRES), not from the file on disk.
        byte[] bytes = edited ?? ReadModel(modelPath, romfsPaths);
        string name = RomfsFiles.StemOf(modelPath);
        string hash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(bytes))[..12];
        string models = Path.Combine(cache, "models");
        Directory.CreateDirectory(models);
        string stem = Path.Combine(models, $"{name}-{hash}");
        string pack = stem + ".bfap", shaders = stem + ".bfsp";
        if (File.Exists(pack) && File.Exists(shaders)) return new PreparedModel(pack, shaders, name);

        stage?.Report("Preparing the model's materials and textures");
        ProgramUse? use = null;
        AssetResult result;
        using (FileStream output = File.Create(pack + ".partial"))
            result = ModelAssetPreparer.Prepare(new AssetOptions(romfs, [], Storage: TextureStorage.Linear), name, bytes, [], output, programs => use = programs);
        if (result.Failures.Count > 0) throw new InvalidDataException($"{result.Failures[0].File}: {result.Failures[0].Reason}");
        PrepareShaders(romfs, cache, pack + ".partial", name, use ?? new ProgramUse(), shaders, stage);
        File.Move(pack + ".partial", pack, true);
        return new PreparedModel(pack, shaders, name);
    }

    /// <summary>A model file's bytes as plain BFRES: the mesh codec unpacked, Yaz0 and zstd undone. Zstd needs the dictionaries of a game's <c>Pack/ZsDic.pack.zs</c>: each source is tried, then the folders above the file.</summary>
    public static byte[] ReadModel(string path, IEnumerable<string> sources)
    {
        byte[] data = File.ReadAllBytes(path);
        if (path.EndsWith(".mc", StringComparison.OrdinalIgnoreCase))
            return McSharp.MeshCodec.DecompressMc(data, out McSharp.McStatus status) ?? throw new InvalidDataException($"{Path.GetFileName(path)} could not be unpacked from the mesh codec ({status}).");
        if (Yaz0.IsCompressed(data)) return Yaz0.Decompress(data);
        if (!ZsDic.IsCompressed(data)) return data;
        HashSet<string> tried = new(StringComparer.OrdinalIgnoreCase);
        foreach (string source in sources.Concat(Above(path)))
            if (tried.Add(Path.TrimEndingDirectorySeparator(source)) && TryDecompress(data, path, source) is { } result) return result;
        throw new InvalidDataException($"{Path.GetFileName(path)} is compressed and none of your romfs paths can open it. Add the game's romfs folder in Preferences > Game.");
    }

    private static IEnumerable<string> Above(string path)
    {
        for (DirectoryInfo? dir = new(Path.GetDirectoryName(Path.GetFullPath(path))!); dir is not null; dir = dir.Parent)
            if (File.Exists(Path.Combine(dir.FullName, "Pack", "ZsDic.pack.zs")))
            {
                yield return dir.FullName;
                yield break;
            }
    }

    private static byte[]? TryDecompress(byte[] data, string path, string folder)
    {
        try
        {
            if (!File.Exists(Path.Combine(folder, "Pack", "ZsDic.pack.zs"))) return null;
            using ZsDic dictionaries = ZsDic.FromRomfs(folder);
            return dictionaries.Decompress(data, path);
        }
        catch (Exception e) when (e is not OutOfMemoryException)
        {
            return null;
        }
    }

    // The Model shader archive, cut down to the programs the renderer will pick for this model. The materials name one program each, but the renderer also draws every material in other passes
    // (depth, shadow, cubemap) and for the skin weight counts of its shapes, and picks a program for each from the shading model's options. So the archive is prepared once for the materials'
    // own programs, which is quick, and that archive's options are then used to work out the full set the renderer will ask for; it is prepared again for that set.
    private static void PrepareShaders(string romfs, string cache, string pack, string modelName, ProgramUse use, string output, IProgress<string>? stage)
    {
        using RomfsFiles files = new(romfs);
        string path = files.ShaderFiles().FirstOrDefault(p => Path.GetFileName(p).StartsWith("Model.", StringComparison.OrdinalIgnoreCase))
            ?? throw new FileNotFoundException("The folder has no Shader/Model shader archive; pick the romfs folder of the game.");
        // The archive is named by the start of its file name. Prepared archives are kept for the programs they were prepared for, so a model that wants programs prepared before (for another model) copies the
        // file and never reads the big archive.
        string name = Path.GetFileName(path).Split('.')[0];
        stage?.Report("Preparing the shaders this model uses");
        Prepared(files, cache, path, name, use.Of(name), output);
        stage?.Report("Working out the shaders for every pass");
        ProgramUse all = Passes(pack, output, modelName, name, use);
        stage?.Report("Preparing the shaders for every pass");
        Prepared(files, cache, path, name, all.Of(name), output);
    }

    private static string? SharedFile(string cache, FileInfo info, string name, IReadOnlyDictionary<string, IReadOnlySet<int>> wanted)
    {
        if (wanted.Count == 0) return null;
        System.Text.StringBuilder key = new();
        key.Append("1|").Append(info.Name).Append('|').Append(info.Length).Append('|').Append(info.LastWriteTimeUtc.Ticks);
        foreach ((string model, IReadOnlySet<int> programs) in wanted.OrderBy(w => w.Key, StringComparer.Ordinal))
            key.Append('|').Append(model).Append(':').Append(string.Join(',', programs.Order()));
        string hash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(key.ToString())))[..24];
        return Path.Combine(cache, "programs", $"{name}-{hash}.bfsp");
    }

    private static readonly object Gate = new();
    private static (string Path, long Length, long Ticks, BfshaFile File)? _lastArchive;

    private static BfshaFile Archive(RomfsFiles files, string path, FileInfo info)
    {
        lock (Gate)
        {
            if (_lastArchive is { } known && known.Path == path && known.Length == info.Length && known.Ticks == info.LastWriteTimeUtc.Ticks) return known.File;
        }

        BfshaFile read = BfshaFile.Read(files.Read(path));
        lock (Gate) _lastArchive = (path, info.Length, info.LastWriteTimeUtc.Ticks, read);
        return read;
    }

    // Writes the archive prepared for the wanted programs to <output>: copied from the shared folder when it was prepared before, else prepared and kept there.
    private static void Prepared(RomfsFiles files, string cache, string path, string name, IReadOnlyDictionary<string, IReadOnlySet<int>> wanted, string output)
    {
        FileInfo info = new(path);
        string? shared = info.Exists ? SharedFile(cache, info, name, wanted) : null;
        if (shared is not null && File.Exists(shared))
        {
            File.Copy(shared, output, true);
            return;
        }

        BfshaFile archive = info.Exists ? Archive(files, path, info) : BfshaFile.Read(files.Read(path));
        using (FileStream stream = File.Create(output + ".partial")) ArchivePreparer.Prepare(archive, stream, 5, 0, null, wanted);
        File.Move(output + ".partial", output, true);
        if (shared is null || archive.Name != name) return;
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(shared)!);
            string temporary = shared + "." + Environment.ProcessId + ".tmp";
            File.Copy(output, temporary, true);
            File.Move(temporary, shared, true);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { }
    }

    private static ProgramUse Passes(string pack, string shaders, string modelName, string archiveName, ProgramUse use)
    {
        ProgramUse all = new();
        foreach ((string model, IReadOnlySet<int> programs) in use.Of(archiveName))
            foreach (int program in programs) all.Add(archiveName, model, program);
        using ContentSet content = new(new CollectingDiagnostics());
        content.AddPack(pack);
        content.AddShaders(archiveName, shaders);
        ModelRecord record = content.FindModel(modelName) ?? throw new InvalidDataException($"The prepared pack has no model {modelName}.");
        PassProgramSelector selector = new(content, PassOptions.Standalone);
        for (int m = 0; m < record.Materials.Count; m++)
        {
            MaterialRecord material = content.Material(record.Materials[m].Material);
            HashSet<int> skins = [0];
            foreach (ShapeRef shape in record.Shapes.Where(s => s.MaterialIndex == m)) skins.Add(content.Shape(shape.Shape).SkinCount);
            foreach (RenderPass pass in Enum.GetValues<RenderPass>())
                foreach (int skin in skins) all.Add(material.ShaderArchive, material.ShadingModel, selector.Select(material, pass, skin));
        }

        return all;
    }
}
