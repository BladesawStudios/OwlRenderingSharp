using System.Numerics;
using OwlRenderingSharp.Content.Geometry;

namespace OwlRenderingSharp.Server;

/// <summary>A sphere in the vertex layout of a real shape, so a material's own shader program draws it.</summary>
/// <remarks>
/// A <see cref="ShapeGeometry"/> stores every attribute of every vertex as four 32-bit words, in the order of the shader input locations. The sphere takes the first vertex of a shape that uses the
/// material as its template, so attributes the sphere has no value for (colour, skin weights and indices) keep that vertex's values, and then overwrites the position, normal, tangent, bitangent and
/// texture coordinates by name. The UVs run once around and once from pole to pole. The result is centred on the origin of the shape's own space; <see cref="Placement"/> says where that lands.
/// </remarks>
public static class PreviewSphere
{
    public const int Rings = 32, Segments = 48;

    public static ShapeGeometry Build(ShapeGeometry template, IReadOnlyDictionary<string, int> locationsByName, float radius)
    {
        int attributes = template.Locations.Count, count = (Rings + 1) * (Segments + 1);
        Dictionary<int, string> names = [];
        foreach ((string name, int location) in locationsByName) names[location] = name;
        byte[] vertices = new byte[count * attributes * 16];
        for (int v = 0; v < count; v++)
        {
            int ring = v / (Segments + 1), segment = v % (Segments + 1);
            float polar = MathF.PI * ring / Rings, around = MathF.Tau * segment / Segments;
            Vector3 normal = new(MathF.Sin(polar) * MathF.Sin(around), MathF.Cos(polar), MathF.Sin(polar) * MathF.Cos(around));
            Vector3 tangent = new(MathF.Cos(around), 0f, -MathF.Sin(around)), bitangent = Vector3.Cross(normal, tangent);
            Vector2 uv = new((float)segment / Segments, (float)ring / Rings);
            for (int a = 0; a < attributes; a++)
            {
                int at = (v * attributes + a) * 16;
                // The template's first vertex is the default for everything the sphere does not set.
                if (template.VertexCount > 0) Buffer.BlockCopy(template.Vertices, a * 16, vertices, at, 16);
                string name = names.GetValueOrDefault(template.Locations[a], "");
                switch (name)
                {
                    case "_p0": Set(vertices, at, normal * radius, 1f); break;
                    case "_n0": Set(vertices, at, normal, 0f); break;
                    case "_t0": Set(vertices, at, tangent, 1f); break;
                    case "_b0": Set(vertices, at, bitangent, 0f); break;
                    default:
                        if (name.StartsWith("_u", StringComparison.Ordinal)) Set(vertices, at, new Vector3(uv, 0f), 1f);
                        break;
                }
            }
        }

        // Counter-clockwise seen from outside, two triangles to each quad.
        int quads = Rings * Segments;
        byte[] indices = new byte[quads * 6 * 2];
        int w = 0;
        for (int r = 0; r < Rings; r++)
            for (int s = 0; s < Segments; s++)
            {
                ushort a = (ushort)(r * (Segments + 1) + s), b = (ushort)(a + Segments + 1);
                foreach (ushort i in new[] { a, b, (ushort)(a + 1), (ushort)(a + 1), b, (ushort)(b + 1) })
                {
                    BitConverter.TryWriteBytes(indices.AsSpan(w), i);
                    w += 2;
                }
            }

        return new ShapeGeometry(count, template.Locations, vertices, [new MeshGeometry(quads * 6, false, indices, 0)]);
    }

    /// <summary>Where the sphere, drawn at the origin of the shape's space, ends up in the model: the shape's bone matrix for an unskinned shape, a rigid bone's for a rigidly skinned one, else none.</summary>
    public static Matrix4x4 Placement(int skinCount, int boneIndex, int firstMatrixIndex, IReadOnlyList<Matrix4x4> worlds, int smoothMatrixCount, IReadOnlyList<ushort> matrixToBone)
    {
        if (skinCount == 0) return boneIndex >= 0 && boneIndex < worlds.Count ? worlds[boneIndex] : Matrix4x4.Identity;
        if (skinCount == 1 && firstMatrixIndex >= smoothMatrixCount && firstMatrixIndex < matrixToBone.Count)
        {
            int bone = matrixToBone[firstMatrixIndex];
            return bone >= 0 && bone < worlds.Count ? worlds[bone] : Matrix4x4.Identity;
        }

        return Matrix4x4.Identity;
    }

    private static void Set(byte[] bytes, int at, Vector3 value, float w)
    {
        BitConverter.TryWriteBytes(bytes.AsSpan(at), value.X);
        BitConverter.TryWriteBytes(bytes.AsSpan(at + 4), value.Y);
        BitConverter.TryWriteBytes(bytes.AsSpan(at + 8), value.Z);
        BitConverter.TryWriteBytes(bytes.AsSpan(at + 12), w);
    }
}
