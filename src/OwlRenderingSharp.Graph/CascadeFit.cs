using System.Numerics;

namespace OwlRenderingSharp.Graph;

/// <summary>Splits a camera's view frustum into slices and fits an orthographic light camera around each.</summary>
/// <remarks>
/// Split distances blend a uniform and a logarithmic spacing between the camera's near plane and the shadow distance. Each slice is wrapped in a sphere, so the light camera's size
/// does not change as the view turns, and the sphere's centre is snapped to the shadow map's texel grid, so the shadow edge does not shimmer as the camera moves. The light camera
/// looks from the sun along the direction to the slice centre and reaches back by one radius to catch casters outside the slice. It is pure arithmetic and holds no state.
/// </remarks>
public static class CascadeFit
{
    private const float LogBlend = 0.75f;

    public static IReadOnlyList<ShadowCascade> Fit(Camera camera, float aspect, Vector3 toSun, int count, float shadowDistance, int resolution)
    {
        if (count <= 0) throw new ArgumentOutOfRangeException(nameof(count), "A cascade set needs at least one slice");
        Vector3 sun = Vector3.Normalize(toSun);
        float near = camera.NearPlane, far = Math.Min(camera.FarPlane, shadowDistance);
        List<ShadowCascade> cascades = [];
        float start = near;
        for (int i = 1; i <= count; i++)
        {
            float end = Split(near, far, i, count);
            cascades.Add(Slice(camera, aspect, sun, start, end, resolution));
            start = end;
        }

        return cascades;
    }

    public static float Split(float near, float far, int index, int count)
    {
        float t = (float)index / count;
        return LogBlend * near * MathF.Pow(far / near, t) + (1f - LogBlend) * (near + (far - near) * t);
    }

    private static ShadowCascade Slice(Camera camera, float aspect, Vector3 sun, float start, float end, int resolution)
    {
        Vector3[] corners = Corners(camera, aspect, start, end);
        Vector3 center = Vector3.Zero;
        foreach (Vector3 corner in corners) center += corner / corners.Length;
        float radius = corners.Max(c => Vector3.Distance(c, center));
        radius = MathF.Ceiling(radius * 16f) / 16f;

        Vector3 up = MathF.Abs(sun.Y) > 0.99f ? Vector3.UnitZ : Vector3.UnitY;
        Vector3 right = Vector3.Normalize(Vector3.Cross(up, sun)), lightUp = Vector3.Cross(sun, right);
        float texel = 2f * radius / resolution;
        center -= right * Remainder(Vector3.Dot(center, right), texel) + lightUp * Remainder(Vector3.Dot(center, lightUp), texel);

        Camera light = new()
        {
            Eye = center + sun * (radius * 2f),
            Target = center,
            Up = lightUp,
            OrthographicHeight = 2f * radius,
            NearPlane = 0f,
            FarPlane = radius * 4f,
        };
        Matrix4x4 bias = Matrix4x4.CreateScale(0.5f, 0.5f, 1f) * Matrix4x4.CreateTranslation(0.5f, 0.5f, 0f);
        return new ShadowCascade(light, light.ViewMatrix() * light.ProjectionMatrix(1f) * bias, end);
    }

    private static float Remainder(float value, float step) => value - MathF.Round(value / step) * step;

    private static Vector3[] Corners(Camera camera, float aspect, float near, float far)
    {
        Matrix4x4.Invert(camera.ViewMatrix(), out Matrix4x4 world);
        float tanY = MathF.Tan(camera.FovRadians / 2f), tanX = tanY * aspect;
        Vector3[] corners = new Vector3[8];
        int n = 0;
        foreach (float depth in new[] { near, far })
            foreach (float sx in new[] { -1f, 1f })
                foreach (float sy in new[] { -1f, 1f })
                    corners[n++] = Vector3.Transform(new Vector3(sx * tanX * depth, sy * tanY * depth, -depth), world);
        return corners;
    }
}
