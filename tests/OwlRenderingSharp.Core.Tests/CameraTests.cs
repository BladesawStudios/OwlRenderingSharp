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

    [Fact]
    public void AnOrthographicCameraMapsItsVolumeToTheUnitBoxWithZeroToOneDepth()
    {
        Camera camera = new() { Eye = Vector3.Zero, Target = -Vector3.UnitZ, NearPlane = 1, FarPlane = 11, OrthographicHeight = 4 };
        Matrix4x4 projection = camera.ProjectionMatrix(2f);
        Vector4 near = Vector4.Transform(new Vector4(4, 2, -1, 1), projection);
        Vector4 far = Vector4.Transform(new Vector4(-4, -2, -11, 1), projection);
        Assert.Equal((1f, 1f, 0f, 1f), (near.X, near.Y, near.Z, near.W));
        Assert.Equal((-1f, -1f, 1f, 1f), (far.X, far.Y, far.Z, far.W));
    }
}
