using BfShaderSharp.Errors;
using BfShaderSharp.Pack;
using BfShaderSharp.Pack.Records;
using OwlRenderingSharp.Content.Geometry;

namespace OwlRenderingSharp.Content.Tests;

public class ShapeGeometryBuilderTests
{
    private static readonly BlobKey StreamKey = new(1, 1), IndexKey = new(2, 2);

    // Three vertices of a float3 position and four index bytes, 16 bytes apart.
    private static byte[] StreamBytes()
    {
        List<byte> bytes = [];
        for (int v = 0; v < 3; v++)
        {
            bytes.AddRange(BitConverter.GetBytes((float)v));
            bytes.AddRange(BitConverter.GetBytes(0f));
            bytes.AddRange(BitConverter.GetBytes(0f));
            bytes.AddRange([(byte)(10 + v), 0, 0, 0]);
        }

        return [.. bytes];
    }

    private static byte[] Blob(BlobKey key) => key == StreamKey ? StreamBytes() : [0, 0, 1, 0, 2, 0];

    private static ShapeRecord Shape(uint primitive = 4, uint indexFormat = 1) => new(
        3, 1,
        [new AttribRecord("_p0", 0x811, 0, 0), new AttribRecord("_i0", 0x10a, 12, 0), new AttribRecord("_u9", 0x811, 0, 0)],
        [new StreamRecord(16, StreamKey)],
        [new MeshRecord(primitive, indexFormat, 0, [], IndexKey, 3)],
        [], [], []);

    private static readonly Dictionary<string, int> Locations = new() { ["_p0"] = 0, ["_i0"] = 6 };

    [Fact]
    public void OnlyTheMappedAttributesAreKeptInLocationOrder()
    {
        ShapeGeometry geometry = ShapeGeometryBuilder.Build(Shape(), Locations, Blob);
        Assert.Equal([0, 6], geometry.Locations);
        Assert.Equal(3 * 2 * 16, geometry.Vertices.Length);
        Assert.Equal(32, geometry.Stride);
    }

    [Fact]
    public void EachVertexHoldsItsDecodedAttributes()
    {
        ShapeGeometry geometry = ShapeGeometryBuilder.Build(Shape(), Locations, Blob);
        Assert.Equal(2f, BitConverter.ToSingle(geometry.Vertices, 2 * 32));
        Assert.Equal(12u, BitConverter.ToUInt32(geometry.Vertices, 2 * 32 + 16));
    }

    [Fact]
    public void MeshesKeepTheirIndices()
    {
        MeshGeometry mesh = ShapeGeometryBuilder.Build(Shape(), Locations, Blob).Meshes[0];
        Assert.Equal((3, false), (mesh.IndexCount, mesh.Wide));
        Assert.Equal(6, mesh.Indices.Length);
    }

    [Fact]
    public void ANonTriangleShapeAndAByteIndexAreUnsupported()
    {
        Assert.Throws<BfShaderUnsupportedException>(() => ShapeGeometryBuilder.Build(Shape(primitive: 6), Locations, Blob));
        Assert.Throws<BfShaderUnsupportedException>(() => ShapeGeometryBuilder.Build(Shape(indexFormat: 0), Locations, Blob));
    }

    [Fact]
    public void AStreamTooShortForItsVerticesIsMalformed()
    {
        Assert.Throws<BfShaderFormatException>(() => ShapeGeometryBuilder.Build(Shape(), Locations, key => key == StreamKey ? new byte[20] : Blob(key)));
    }

    [Fact]
    public void AnAttributeOutsideItsStrideIsMalformed()
    {
        ShapeRecord wide = Shape() with { Streams = [new StreamRecord(8, StreamKey)] };
        Assert.Throws<BfShaderFormatException>(() => ShapeGeometryBuilder.Build(wide, Locations, Blob));
    }
}
