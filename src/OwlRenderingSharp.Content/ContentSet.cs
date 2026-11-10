using BfShaderSharp.Pack;
using BfShaderSharp.Pack.Records;
using BfShaderSharp.Prepared;
using OwlRenderingSharp.Content.Textures;

namespace OwlRenderingSharp.Content;

/// <summary>The packs and prepared shader archives a renderer draws from, searched as one.</summary>
/// <remarks>
/// A pack added later wins over one added earlier, so a modded model's pack opened beside the game's pack shadows the game's models of the
/// same name. A blob is found in the newest pack that holds it. Shader archives are kept by the name the host gives them (for example
/// <c>Model</c>). The set owns what it is given and disposes it. Reads are safe from many threads once adding is done.
/// </remarks>
public sealed class ContentSet(IRenderDiagnostics diagnostics) : IDisposable
{
    private readonly List<AssetPack> _packs = [];
    private readonly Dictionary<string, PreparedArchive> _shaders = new(StringComparer.Ordinal);

    public IReadOnlyList<AssetPack> Packs => _packs;

    public AssetPack AddPack(string path)
    {
        AssetPack pack = AssetPack.Open(path);
        _packs.Add(pack);
        return pack;
    }

    public PreparedArchive AddShaders(string name, string path)
    {
        PreparedArchive archive = PreparedArchive.Open(path);
        _shaders.Add(name, archive);
        return archive;
    }

    public PreparedArchive Shaders(string name) =>
        _shaders.TryGetValue(name, out PreparedArchive? archive) ? archive : throw new KeyNotFoundException($"No shader archive named {name} was added");

    public ModelRecord? FindModel(string name) => Newest(pack => pack.Model(name));

    // A texture the packs lack is reported and left to the caller's neutral texture.
    public TextureRecord? FindTexture(string name)
    {
        TextureRecord? texture = Newest(pack => pack.Texture(name));
        if (texture is null) diagnostics.Report(new RenderDiagnostic(DiagnosticSeverity.Warning, "No pack has this texture; the neutral texture is used", Resource: name));
        return texture;
    }

    public DecodedTexture DecodeTexture(TextureRecord record) => TextureDecoding.Decode(record, Blob(record.File).Bytes);

    public MaterialRecord Material(BlobKey key) => Holding(key).Material(key);

    public ShapeRecord Shape(BlobKey key) => Holding(key).Shape(key);

    public SkeletonRecord Skeleton(BlobKey key) => Holding(key).Skeleton(key);

    public UniformLayoutRecord UniformLayout(BlobKey key) => Holding(key).UniformLayout(key);

    public BlobData Blob(BlobKey key) => Holding(key).Blob(key);

    public void Dispose()
    {
        foreach (AssetPack pack in _packs) pack.Dispose();
        foreach (PreparedArchive archive in _shaders.Values) archive.Dispose();
    }

    private AssetPack Holding(BlobKey key) =>
        _packs.LastOrDefault(pack => pack.Has(key)) ?? throw new KeyNotFoundException($"No pack holds blob {key}");

    private T? Newest<T>(Func<AssetPack, T?> find) where T : class
    {
        for (int i = _packs.Count - 1; i >= 0; i--)
            if (find(_packs[i]) is { } found) return found;
        return null;
    }
}
