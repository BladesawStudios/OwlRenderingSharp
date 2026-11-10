using System.Numerics;

namespace OwlRenderingSharp.Graph.Tests;

public class CascadeFitTests
{
    private static readonly Vector3 ToSun = Vector3.Normalize(new Vector3(0.4f, 1f, 0.3f));

    private static Camera Viewer => new() { Eye = new Vector3(0, 2, 10), Target = new Vector3(0, 1, 0), NearPlane = 0.5f, FarPlane = 1000f, FovRadians = float.DegreesToRadians(60f) };

    [Fact]
    public void TheSplitsGrowAndTheLastOneIsTheShadowDistance()
    {
        IReadOnlyList<ShadowCascade> cascades = CascadeFit.Fit(Viewer, 1.5f, ToSun, 4, 80f, 1024);
        Assert.Equal(4, cascades.Count);
        Assert.True(cascades.Zip(cascades.Skip(1), (a, b) => a.Split < b.Split).All(ok => ok));
        Assert.Equal(80f, cascades[^1].Split, 1e-3f);
    }

    [Fact]
    public void EveryPointOfASliceLandsInsideItsShadowTexture()
    {
        Camera viewer = Viewer;
        IReadOnlyList<ShadowCascade> cascades = CascadeFit.Fit(viewer, 1.5f, ToSun, 3, 60f, 1024);
        Matrix4x4.Invert(viewer.ViewMatrix(), out Matrix4x4 world);
        float start = viewer.NearPlane, tanY = MathF.Tan(viewer.FovRadians / 2f);
        foreach (ShadowCascade cascade in cascades)
        {
            foreach (float depth in new[] { start, cascade.Split })
                foreach (float sx in new[] { -1f, 0f, 1f })
                    foreach (float sy in new[] { -1f, 1f })
                    {
                        Vector3 point = Vector3.Transform(new Vector3(sx * tanY * 1.5f * depth, sy * tanY * depth, -depth), world);
                        Vector4 texture = Vector4.Transform(new Vector4(point, 1f), cascade.TextureMatrix);
                        Assert.InRange(texture.X, 0f, 1f);
                        Assert.InRange(texture.Y, 0f, 1f);
                        Assert.InRange(texture.Z, 0f, 1f);
                    }

            start = cascade.Split;
        }
    }

    [Fact]
    public void TheLightLooksAlongTheSunDirectionFromAboveTheSlice()
    {
        ShadowCascade cascade = CascadeFit.Fit(Viewer, 1.5f, ToSun, 1, 40f, 1024)[0];
        Vector3 forward = Vector3.Normalize(cascade.Light.Target - cascade.Light.Eye);
        Assert.Equal(-ToSun.X, forward.X, 1e-4f);
        Assert.Equal(-ToSun.Y, forward.Y, 1e-4f);
        Assert.True(cascade.Light.OrthographicHeight > 0f);
    }

    [Fact]
    public void MovingTheViewerByLessThanATexelKeepsTheShadowGridPut()
    {
        Camera moved = Viewer;
        moved.Eye += new Vector3(0.001f, 0f, 0f);
        moved.Target += new Vector3(0.001f, 0f, 0f);
        ShadowCascade first = CascadeFit.Fit(Viewer, 1.5f, ToSun, 1, 40f, 1024)[0], second = CascadeFit.Fit(moved, 1.5f, ToSun, 1, 40f, 1024)[0];
        Vector4 a = Vector4.Transform(new Vector4(1, 1, 1, 1), first.TextureMatrix), b = Vector4.Transform(new Vector4(1, 1, 1, 1), second.TextureMatrix);
        Assert.Equal(a.X, b.X, 1f / 1024f);
        Assert.Equal(a.Y, b.Y, 1f / 1024f);
    }

    [Fact]
    public void ANeedlessCountIsRefused()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CascadeFit.Fit(Viewer, 1f, ToSun, 0, 40f, 1024));
    }
}
