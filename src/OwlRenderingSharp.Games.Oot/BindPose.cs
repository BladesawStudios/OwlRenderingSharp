using System.Numerics;
using BfShaderSharp.Pack.Records;

namespace OwlRenderingSharp.Games.Oot;

/// <summary>The world matrix of every bone of a skeleton in its rest pose.</summary>
/// <remarks>
/// A bone's local matrix is its scale, then its rotation, then its position. The rotation is a quaternion unless the bone's flags carry the Euler bit (1 &lt;&lt; 12),
/// in which case it is Euler XYZ in radians. A bone's parent comes before it in the skeleton, so one pass is enough; a parent index outside the skeleton makes the bone a root.
/// </remarks>
public static class BindPose
{
    private const uint EulerFlag = 1u << 12;

    public static Matrix4x4[] Worlds(SkeletonRecord skeleton)
    {
        Matrix4x4[] worlds = new Matrix4x4[skeleton.Bones.Count];
        for (int i = 0; i < worlds.Length; i++)
        {
            BoneRecord bone = skeleton.Bones[i];
            Matrix4x4 local = Matrix4x4.CreateScale(bone.Scale[0], bone.Scale[1], bone.Scale[2]) * Rotation(bone) * Matrix4x4.CreateTranslation(bone.Position[0], bone.Position[1], bone.Position[2]);
            worlds[i] = bone.Parent >= 0 && bone.Parent < i ? local * worlds[bone.Parent] : local;
        }

        return worlds;
    }

    private static Matrix4x4 Rotation(BoneRecord bone)
    {
        float[] r = bone.Rotation;
        if ((bone.Flags & EulerFlag) != 0) return Matrix4x4.CreateRotationX(r[0]) * Matrix4x4.CreateRotationY(r[1]) * Matrix4x4.CreateRotationZ(r[2]);
        return Matrix4x4.CreateFromQuaternion(Quaternion.Normalize(new Quaternion(r[0], r[1], r[2], r[3])));
    }
}
