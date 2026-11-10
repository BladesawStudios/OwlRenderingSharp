using System.Numerics;
using BfShaderSharp.Pack.Records;

namespace OwlRenderingSharp.Games.Oot;

/// <summary>A camera that looks at a skeleton's rest pose from the front, far enough back to see all of its bones.</summary>
/// <remarks>The target is the middle of the box around the bone positions, and the camera sits two and a half radii of that box in front of it (positive z).</remarks>
public static class BindPoseCamera
{
    public static Camera Frame(SkeletonRecord skeleton)
    {
        Vector3 min = new(float.MaxValue), max = new(float.MinValue);
        foreach (Matrix4x4 world in BindPose.Worlds(skeleton))
        {
            min = Vector3.Min(min, world.Translation);
            max = Vector3.Max(max, world.Translation);
        }

        if (min.X > max.X) return new Camera();
        Vector3 center = (min + max) / 2f;
        float radius = Math.Max(0.1f, (max - min).Length() / 2f);
        return new Camera { Target = center, Eye = center + new Vector3(0, 0, radius * 2.5f), NearPlane = radius * 0.05f, FarPlane = radius * 20f };
    }
}
