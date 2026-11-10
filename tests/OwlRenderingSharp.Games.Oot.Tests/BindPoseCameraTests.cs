using System.Numerics;
using BfShaderSharp.Pack.Records;

namespace OwlRenderingSharp.Games.Oot.Tests;

public class BindPoseCameraTests
{
    private static BoneRecord Bone(float y) => new("b", -1, 0, -1, -1, -1, [1f, 1f, 1f], [0f, 0f, 0f, 1f], [0f, y, 0f]);

    [Fact]
    public void TheCameraLooksAtTheMiddleOfTheBonesFromTheFront()
    {
        Camera camera = BindPoseCamera.Frame(new SkeletonRecord(0, [Bone(0f), Bone(2f)], [], []));
        Assert.Equal(new Vector3(0, 1, 0), camera.Target);
        Assert.True(camera.Eye.Z > 0f);
        Assert.Equal(1f, camera.Eye.Y);
    }

    [Fact]
    public void ASkeletonWithoutBonesGetsTheDefaultCamera()
    {
        Assert.Equal(new Camera().Eye, BindPoseCamera.Frame(new SkeletonRecord(0, [], [], [])).Eye);
    }
}
