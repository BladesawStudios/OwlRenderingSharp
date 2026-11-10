using OwlRenderingSharp.Content.Geometry;

namespace OwlRenderingSharp.Gpu;

/// <summary>The GPU form of a <see cref="ShapeGeometry"/>: its vertex buffer and one <see cref="GpuMesh"/> for each level of detail.</summary>
/// <remarks>It owns every buffer and vertex array it creates and releases them together when disposed.</remarks>
public sealed class GpuGeometry : IDisposable
{
    private readonly GpuBuffer _vertices;
    private readonly List<GpuMesh> _meshes = [];

    public GpuGeometry(GpuDevice device, string name, ShapeGeometry geometry)
    {
        _vertices = new GpuBuffer(device, $"{name} vertices", Math.Max(16, geometry.Vertices.Length));
        if (geometry.Vertices.Length > 0) _vertices.Write<byte>(geometry.Vertices);
        for (int i = 0; i < geometry.Meshes.Count; i++)
        {
            MeshGeometry mesh = geometry.Meshes[i];
            int bytes = mesh.IndexCount * (mesh.Wide ? 4 : 2);
            GpuBuffer indices = new(device, $"{name} indices {i}", Math.Max(16, bytes));
            if (bytes > 0) indices.Write<byte>(mesh.Indices.AsSpan(0, bytes));
            _meshes.Add(new GpuMesh(device, $"{name} mesh {i}", _vertices, geometry.Stride, geometry.Locations, indices, mesh.IndexCount, mesh.Wide, mesh.FirstVertex));
        }
    }

    public IReadOnlyList<GpuMesh> Meshes => _meshes;

    public void Dispose()
    {
        foreach (GpuMesh mesh in _meshes) mesh.Dispose();
        _vertices.Dispose();
    }
}
