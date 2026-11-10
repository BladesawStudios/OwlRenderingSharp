using System.Numerics;

namespace OwlRenderingSharp.Core.Tests;

public class CameraTests
{
    [Fact]
    public void ThePointAtTheNearPlaneMapsToMinusOneInGlDepth()
    {
        Camera camera = new() { Eye = Vector3.Zero, Target = -Vector3.UnitZ, NearPlane = 1, FarPlane = 100 };
        Vector4 near = Vector4.Transform(new Vector4(0, 0, -1, 1), camera.ProjectionMatrixGl(1));
        Vector4 far = Vector4.Transform(new Vector4(0, 0, -100, 1), camera.ProjectionMatrixGl(1));
        Assert.Equal(-1f, near.Z / near.W, 1e-4f);
        Assert.Equal(1f, far.Z / far.W, 1e-4f);
    }

    [Fact]
    public void TheViewMatrixLooksDownTheTarget()
    {
        Camera camera = new() { Eye = new Vector3(0, 0, 5), Target = Vector3.Zero };
        Vector3 view = Vector3.Transform(Vector3.Zero, camera.ViewMatrix());
        Assert.Equal(-5f, view.Z, 1e-5f);
    }
}
