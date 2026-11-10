using System.Numerics;

namespace OwlRenderingSharp;

/// <summary>Matrices that convert between System.Numerics clip space (depth 0 to 1) and OpenGL's default (-1 to 1).</summary>
/// <remarks>The world is right-handed and Y-up as the games' is; only the depth range is converted, and only here.</remarks>
public static class ClipSpace
{
    public static readonly Matrix4x4 ZeroToOneDepthToGl = Matrix4x4.CreateScale(1, 1, 2) * Matrix4x4.CreateTranslation(0, 0, -1);
}
