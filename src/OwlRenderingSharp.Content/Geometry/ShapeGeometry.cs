namespace OwlRenderingSharp.Content.Geometry;

/// <summary>The geometry of a shape in the form the GPU takes: one block of vertices with 16 bytes for each attribute, and its levels of detail.</summary>
/// <remarks>
/// Vertices are interleaved: for every vertex, the attributes in the order of <see cref="Locations"/>, each as four 32-bit words (see
/// <see cref="VertexDecoder"/>). <see cref="Locations"/> are the shader input locations the attributes feed. It holds no GL object and is the same
/// for every model that uses the shape.
/// </remarks>
public sealed record ShapeGeometry(int VertexCount, IReadOnlyList<int> Locations, byte[] Vertices, IReadOnlyList<MeshGeometry> Meshes)
{
    public int Stride => Locations.Count * 16;
}
