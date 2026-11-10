using System.Numerics;
using BfShaderSharp.Pack.Records;

namespace OwlRenderingSharp.Games.Oot.Tests;

public class BindPoseTests
{
    private static BoneRecord Bone(int parent, float x, float y, float z, uint flags = 0, float[]? rotation = null) =>
        new("bone", parent, flags, -1, -1, -1, [1f, 1f, 1f], rotation ?? [0f, 0f, 0f, 1f], [x, y, z]);

    private static SkeletonRecord Skeleton(params BoneRecord[] bones) => new(0, bones, [], []);

    [Fact]
    public void AChildBoneIsPlacedRelativeToItsParent()
    {
        Matrix4x4[] worlds = BindPose.Worlds(Skeleton(Bone(-1, 1, 0, 0), Bone(0, 0, 2, 0)));
        Assert.Equal(new Vector3(1, 0, 0), worlds[0].Translation);
        Assert.Equal(new Vector3(1, 2, 0), worlds[1].Translation);
    }

    [Fact]
    public void AnEulerBoneRotatesByItsAnglesInRadians()
    {
        Matrix4x4 world = BindPose.Worlds(Skeleton(Bone(-1, 0, 0, 0, 1u << 12, [0f, 0f, MathF.PI / 2, 0f])))[0];
        Vector3 turned = Vector3.Transform(Vector3.UnitX, world);
        Assert.Equal(0f, turned.X, 1e-5f);
        Assert.Equal(1f, turned.Y, 1e-5f);
    }

    [Fact]
    public void AParentOutsideTheSkeletonMakesTheBoneARoot()
    {
        Matrix4x4[] worlds = BindPose.Worlds(Skeleton(Bone(7, 3, 0, 0)));
        Assert.Equal(new Vector3(3, 0, 0), worlds[0].Translation);
    }
}
