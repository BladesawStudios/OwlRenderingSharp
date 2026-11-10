using OwlRenderingSharp.Blocks;

namespace OwlRenderingSharp.Games.Oot.Tests;

public class OotBlockLayoutsTests
{
    [Fact]
    public void TheEmbeddedLayoutsAreRead()
    {
        OotBlockLayouts layouts = new();
        Assert.Equal(13552, layouts.Find(OotBlockLayouts.Context)!.Size);
        Assert.Equal(64, layouts.Find(OotBlockLayouts.Shape)!.Field("Position").Offset);
        Assert.Equal(1824, layouts.Find(OotBlockLayouts.Environment)!.Size);
        Assert.Null(layouts.Find("gsys_scene_material"));
    }

    [Fact]
    public void TheCameraFieldsSitWhereTheShaderReadsThem()
    {
        BlockLayout context = new OotBlockLayouts().Find(OotBlockLayouts.Context)!;
        Assert.Equal(0, context.Field("View").Offset);
        Assert.Equal(112, context.Field("Projection").Offset);
        Assert.Equal(224, context.Field("NearFar").Offset);
    }

    [Fact]
    public void TheSkeletonLayoutHoldsOneMatrixPerBoneFromByteZero()
    {
        BlockLayout skeleton = new OotBlockLayouts().SkeletonFor(5);
        Assert.Equal(240, skeleton.Size);
        Assert.Equal(96, skeleton.Field(OotBlockLayouts.Bone(2)).Offset);
    }

    [Fact]
    public void ASkeletonLayoutIsBuiltOnceAndAtLeastOneBoneLong()
    {
        OotBlockLayouts layouts = new();
        Assert.Same(layouts.SkeletonFor(3), layouts.SkeletonFor(3));
        Assert.Equal(48, layouts.SkeletonFor(0).Size);
    }
}
