using BfShaderSharp.Pack.Records;
using OwlRenderingSharp.Gpu;

namespace OwlRenderingSharp.Graph.Tests;

public class MaterialDrawStatesTests
{
    private static MaterialRecord Material(params (string Name, string Value)[] infos) => new(
        "Model", "model", 0, 0, [], [.. infos.Select(i => new RenderInfoRecord(i.Name, 2, [], [], [i.Value]))], [], [], [], ReadOnlyMemory<byte>.Empty, -1, -1);

    [Fact]
    public void AMaterialWithoutRenderInfoDrawsOpaque()
    {
        Assert.Equal(DrawState.Opaque, MaterialDrawStates.From(Material()));
    }

    [Theory]
    [InlineData("front", CullMode.Back)]
    [InlineData("back", CullMode.Front)]
    [InlineData("both", CullMode.None)]
    [InlineData("none", CullMode.Both)]
    public void TheDisplayFacePicksTheCulling(string face, CullMode expected)
    {
        Assert.Equal(expected, MaterialDrawStates.From(Material(("gsys_render_state_display_face", face))).Cull);
    }

    [Fact]
    public void TheDepthStateIsRead()
    {
        DrawState state = MaterialDrawStates.From(Material(("gsys_depth_test_enable", "true"), ("gsys_depth_test_write", "false"), ("gsys_depth_test_func", "gequal")));
        Assert.Equal((true, false, DepthFunc.GreaterOrEqual), (state.DepthTest, state.DepthWrite, state.DepthFunc));
        Assert.False(MaterialDrawStates.From(Material(("gsys_depth_test_enable", "false"))).DepthTest);
    }
}
