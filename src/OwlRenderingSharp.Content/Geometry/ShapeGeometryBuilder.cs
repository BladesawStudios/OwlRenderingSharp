using BfShaderSharp.Errors;
using BfShaderSharp.Pack;
using BfShaderSharp.Pack.Records;

namespace OwlRenderingSharp.Content.Geometry;

/// <summary>Builds a <see cref="ShapeGeometry"/> from a shape record and the blobs it names.</summary>
/// <remarks>
/// Only the attributes the caller maps to a location are decoded; the others are not needed by the program the shape is drawn with. The primitive of
/// every level of detail must be triangles. A stream that is shorter than its vertices, an attribute outside its stride, or an unknown format throws
/// <see cref="BfShaderFormatException"/> or <see cref="BfShaderUnsupportedException"/>.
/// </remarks>
public static class ShapeGeometryBuilder
{
    private const uint Triangles = 4;
    private const int IndexByte = 0, IndexUInt16 = 1, IndexUInt32 = 2;

    public static ShapeGeometry Build(ShapeRecord shape, IReadOnlyDictionary<string, int> locationsByName, Func<BlobKey, byte[]> blob)
    {
        List<(AttribRecord Attrib, int Location, VertexFormat Format)> used = [.. shape.Attribs
            .Where(a => locationsByName.ContainsKey(a.Name))
            .Select(a => (a, locationsByName[a.Name], VertexFormat.FromWord(a.Format)))
            .OrderBy(a => a.Item2)];
        int count = checked((int)shape.VertexCount);
        byte[][] streams = [.. shape.Streams.Select(s => blob(s.Data))];
        byte[] vertices = new byte[checked(count * used.Count * 16)];
        Span<uint> words = stackalloc uint[4];
        for (int a = 0; a < used.Count; a++)
        {
            (AttribRecord attrib, _, VertexFormat format) = used[a];
            if (attrib.Stream >= streams.Length) throw new BfShaderFormatException($"Attribute {attrib.Name} uses stream {attrib.Stream}");
            int stride = shape.Streams[attrib.Stream].Stride;
            if (attrib.Offset + format.ByteSize > stride || (long)stride * count > streams[attrib.Stream].Length)
                throw new BfShaderFormatException($"Attribute {attrib.Name} does not fit its stream");
            for (int v = 0; v < count; v++)
            {
                VertexDecoder.Decode(format, streams[attrib.Stream].AsSpan(v * stride + attrib.Offset), words);
                for (int w = 0; w < 4; w++) BitConverter.TryWriteBytes(vertices.AsSpan((v * used.Count + a) * 16 + w * 4), words[w]);
            }
        }

        return new ShapeGeometry(count, [.. used.Select(u => u.Location)], vertices, [.. shape.Meshes.Select(m => Mesh(m, blob))]);
    }

    private static MeshGeometry Mesh(MeshRecord mesh, Func<BlobKey, byte[]> blob)
    {
        if (mesh.Primitive != Triangles) throw new BfShaderUnsupportedException($"Primitive type {mesh.Primitive} is not drawn");
        byte[] indices = blob(mesh.Indices);
        (bool wide, int size) = mesh.IndexFormat switch
        {
            IndexUInt16 => (false, 2),
            IndexUInt32 => (true, 4),
            IndexByte => throw new BfShaderUnsupportedException("Byte indices are not drawn"),
            _ => throw new BfShaderUnsupportedException($"Index format {mesh.IndexFormat} is not read"),
        };
        if ((long)mesh.IndexCount * size > indices.Length) throw new BfShaderFormatException("A mesh has fewer index bytes than its count");
        return new MeshGeometry(checked((int)mesh.IndexCount), wide, indices, checked((int)mesh.FirstVertex));
    }
}
