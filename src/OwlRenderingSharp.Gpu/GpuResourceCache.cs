using BfShaderSharp.Pack;
using BfShaderSharp.Pack.Records;
using OwlRenderingSharp.Content;
using OwlRenderingSharp.Content.Geometry;

namespace OwlRenderingSharp.Gpu;

/// <summary>Creates GPU textures and geometry from the content on first use and shares them by the key of the blob they come from.</summary>
/// <remarks>
/// However many models, materials or shapes name the same texture bytes or the same geometry, one GL object exists. A texture is keyed by its file blob and
/// index in it; geometry by its shape record's key and the attribute layout id the caller gives, since the locations a shape's attributes feed depend on
/// the shading model. It is used from the GL thread only and disposes everything it made.
/// </remarks>
public sealed class GpuResourceCache(GpuDevice device, ContentSet content) : IDisposable
{
    private readonly Dictionary<(BlobKey File, int Index), GpuTexture> _textures = [];
    private readonly Dictionary<(BlobKey Shape, string Layout), GpuGeometry> _geometry = [];

    public int TextureCount => _textures.Count;

    public int GeometryCount => _geometry.Count;

    public GpuTexture Texture(string name, TextureRecord record)
    {
        (BlobKey, int) key = (record.File, record.Index);
        if (!_textures.TryGetValue(key, out GpuTexture? texture))
            _textures[key] = texture = GpuTextureLoader.Create(device, name, content.DecodeTexture(record));
        return texture;
    }

    public GpuGeometry Geometry(string name, BlobKey shape, string layoutId, IReadOnlyDictionary<string, int> locationsByName)
    {
        (BlobKey, string) key = (shape, layoutId);
        if (!_geometry.TryGetValue(key, out GpuGeometry? geometry))
            _geometry[key] = geometry = new GpuGeometry(device, name, ShapeGeometryBuilder.Build(content.Shape(shape), locationsByName, k => content.Bytes(k)));
        return geometry;
    }

    public void Dispose()
    {
        foreach (GpuTexture texture in _textures.Values) texture.Dispose();
        foreach (GpuGeometry geometry in _geometry.Values) geometry.Dispose();
        _textures.Clear();
        _geometry.Clear();
    }
}
