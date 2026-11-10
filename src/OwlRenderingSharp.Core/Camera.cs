using System.Numerics;

namespace OwlRenderingSharp;

/// <summary>A viewpoint in the games' world, perspective by default or orthographic when <see cref="OrthographicHeight"/> is set; the aspect ratio comes from the target it renders into.</summary>
/// <remarks>The matrices are System.Numerics row-vector matrices with depth 0 to 1; <see cref="ProjectionMatrixGl"/> converts the depth for OpenGL.</remarks>
public sealed class Camera
{
    public Vector3 Eye { get; set; } = new(0, 0, 3);

    public Vector3 Target { get; set; } = Vector3.Zero;

    public Vector3 Up { get; set; } = Vector3.UnitY;

    public float FovRadians { get; set; } = float.DegreesToRadians(70f);

    public float NearPlane { get; set; } = 0.1f;

    public float FarPlane { get; set; } = 10000f;

    /// <summary>The height of the view volume in world units; zero keeps the perspective projection and the field of view applies.</summary>
    public float OrthographicHeight { get; set; }

    public Matrix4x4 ViewMatrix() => Matrix4x4.CreateLookAt(Eye, Target, Up);

    public Matrix4x4 ProjectionMatrix(float aspect) => OrthographicHeight > 0f
        ? Matrix4x4.CreateOrthographic(OrthographicHeight * aspect, OrthographicHeight, NearPlane, FarPlane)
        : Matrix4x4.CreatePerspectiveFieldOfView(FovRadians, aspect, NearPlane, FarPlane);

    public Matrix4x4 ProjectionMatrixGl(float aspect) => ProjectionMatrix(aspect) * ClipSpace.ZeroToOneDepthToGl;
}
