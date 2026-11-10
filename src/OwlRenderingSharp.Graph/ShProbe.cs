using System.Numerics;

namespace OwlRenderingSharp.Graph;

/// <summary>Nine-coefficient spherical harmonics of an environment, laid out the way the game's shading programs read a light probe from their first storage buffer.</summary>
/// <remarks>
/// An entry is nine colours of 16 bytes (red, green, blue, one spare float) in the order of the real basis: the constant band, the three linear terms (y, z, x), then the five quadratic ones
/// (xy, yz, 3z^2 - 1, xz, x^2 - y^2). The programs scale the first coefficient by 0.282095 and pick an entry by the shape's light index times 144 bytes, so an entry holds the projection of the radiance
/// (a uniform environment of one has 3.5449 in its first coefficient). The game bakes these into its probe files; here one is projected from any function of direction, by stratified sampling of the sphere.
/// </remarks>
public static class ShProbe
{
    public const int Coefficients = 9, EntryBytes = 144;

    public static Vector3[] Project(Func<Vector3, Vector3> radiance, int rings = 64)
    {
        Vector3[] sums = new Vector3[Coefficients];
        int columns = rings * 2;
        float weight = 4f * MathF.PI / (rings * columns);
        for (int i = 0; i < rings; i++)
            for (int j = 0; j < columns; j++)
            {
                float z = 1f - 2f * (i + 0.5f) / rings, phi = 2f * MathF.PI * (j + 0.5f) / columns, r = MathF.Sqrt(MathF.Max(0f, 1f - z * z));
                Vector3 direction = new(r * MathF.Cos(phi), z, r * MathF.Sin(phi));
                Vector3 color = radiance(direction) * weight;
                float[] basis = Basis(direction);
                for (int k = 0; k < Coefficients; k++) sums[k] += color * basis[k];
            }

        return sums;
    }

    public static float[] Basis(Vector3 d) =>
    [
        0.282095f,
        0.488603f * d.Y, 0.488603f * d.Z, 0.488603f * d.X,
        1.092548f * d.X * d.Y, 1.092548f * d.Y * d.Z, 0.315392f * (3f * d.Z * d.Z - 1f), 1.092548f * d.X * d.Z, 0.546274f * (d.X * d.X - d.Y * d.Y),
    ];

    public static float[] Entry(Vector3[] coefficients)
    {
        float[] floats = new float[Coefficients * 4];
        for (int k = 0; k < Coefficients; k++)
        {
            floats[k * 4] = coefficients[k].X;
            floats[k * 4 + 1] = coefficients[k].Y;
            floats[k * 4 + 2] = coefficients[k].Z;
        }

        return floats;
    }
}
