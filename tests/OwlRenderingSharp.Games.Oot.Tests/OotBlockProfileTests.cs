using System.Numerics;
using BfShaderSharp.Pack.Records;
using OwlRenderingSharp.Blocks;
using OwlRenderingSharp.Graph;

namespace OwlRenderingSharp.Games.Oot.Tests;

public class OotBlockProfileTests
{
    private readonly OotBlockProfile _profile = new();

    private static ShapeInfo Shape(Matrix4x4 world)
    {
        BoneRecord root = new("Root", -1, 0, -1, -1, -1, [1f, 1f, 1f], [0f, 0f, 0f, 1f], [0f, 0f, 0f]);
        BoneRecord rigid = new("Rigid", 0, 0, -1, -1, -1, [1f, 1f, 1f], [0f, 0f, 0f, 1f], [0f, 5f, 0f]);
        SkeletonRecord skeleton = new(0, [root, rigid], [0, 1], [new float[12]]);
        return new ShapeInfo("shape", new ShapeRecord(0, 1, [], [], [], [], [], []), 0, skeleton, world);
    }

    [Theory]
    [InlineData("gsys_context", BlockScope.Frame)]
    [InlineData("gsys_environment", BlockScope.Frame)]
    [InlineData("gsys_shape", BlockScope.Shape)]
    [InlineData("gsys_skeleton", BlockScope.Shape)]
    [InlineData("gsys_material", BlockScope.Material)]
    [InlineData("something_else", BlockScope.Unknown)]
    public void BlocksAreSortedIntoScopes(string block, BlockScope expected)
    {
        Assert.Equal(expected, _profile.Scope(block));
    }

    [Fact]
    public void TheContextCarriesTheCameraMatricesAndNearFar()
    {
        Camera camera = new() { Eye = new Vector3(0, 0, 4), Target = Vector3.Zero, NearPlane = 0.5f, FarPlane = 50f };
        BlockWriter writer = new(_profile.Layout("gsys_context", null)!);
        _profile.Fill("gsys_context", writer, new FrameInfo(camera, 800, 400), null);
        Assert.Equal(0.5f, writer.GetVector4("NearFar").X);
        Assert.Equal(50f, writer.GetVector4("NearFar").Y);
        Assert.Equal(800f, writer.GetVector4("Resolution").X);
    }

    [Fact]
    public void TheShapeBlockCarriesThePlacementTranslation()
    {
        ShapeInfo shape = Shape(Matrix4x4.CreateTranslation(1, 2, 3));
        BlockWriter writer = new(_profile.Layout("gsys_shape", shape)!);
        _profile.Fill("gsys_shape", writer, new FrameInfo(new Camera(), 64, 64), shape);
        Assert.Equal(new Vector4(1, 2, 3, 1), writer.GetVector4("Position"));
    }

    [Fact]
    public void ThePaletteHasOneEntryPerSkeletonMatrixAndRigidMatricesCarryTheirBone()
    {
        ShapeInfo shape = Shape(Matrix4x4.Identity);
        BlockLayout layout = _profile.Layout("gsys_skeleton", shape)!;
        Assert.Equal(96, layout.Size);
        BlockWriter writer = new(layout);
        _profile.Fill("gsys_skeleton", writer, new FrameInfo(new Camera(), 64, 64), shape);
        Assert.Equal(1f, BitConverter.ToSingle(writer.Bytes, 0));
        Assert.Equal(5f, BitConverter.ToSingle(writer.Bytes, 48 + 4 * 7));
    }
}
