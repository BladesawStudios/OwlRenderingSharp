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

    [Fact]
    public void AColorBlendModeReadsTheOperationsFactorsAndConstant()
    {
        MaterialRecord material = new("Model", "model", 0, 0, [],
        [
            new RenderInfoRecord("gsys_render_state_blend_mode", 2, [], [], ["color"]),
            new RenderInfoRecord("gsys_color_blend_rgb_op", 2, [], [], ["max"]),
            new RenderInfoRecord("gsys_color_blend_rgb_src_func", 2, [], [], ["src1_color"]),
            new RenderInfoRecord("gsys_color_blend_rgb_dst_func", 2, [], [], ["one_minus_const_alpha"]),
            new RenderInfoRecord("gsys_color_blend_alpha_op", 2, [], [], ["rev_sub"]),
            new RenderInfoRecord("gsys_color_blend_alpha_src_func", 2, [], [], ["src_alpha_saturate"]),
            new RenderInfoRecord("gsys_color_blend_alpha_dst_func", 2, [], [], ["zero"]),
            new RenderInfoRecord("gsys_color_blend_const_color", 1, [], [0.1f, 0.2f, 0.3f, 0.4f], []),
        ], [], [], [], ReadOnlyMemory<byte>.Empty, -1, -1);
        BlendState blend = MaterialDrawStates.From(material).Blend;
        Assert.True(blend.Enabled);
        Assert.Equal((BlendOp.Max, BlendFactor.Src1Color, BlendFactor.OneMinusConstantAlpha), (blend.RgbOp, blend.RgbSource, blend.RgbDestination));
        Assert.Equal((BlendOp.ReverseSubtract, BlendFactor.SrcAlphaSaturate, BlendFactor.Zero), (blend.AlphaOp, blend.AlphaSource, blend.AlphaDestination));
        Assert.Equal(0.3f, blend.Constant.Z);
        Assert.True(MaterialDrawStates.Blends(material));
    }

    [Fact]
    public void ATranslucentModeBlendsSourceAlphaAndStopsWritingDepth()
    {
        DrawState state = MaterialDrawStates.From(Material(("gsys_render_state_mode", "translucent")));
        Assert.True(state.Blend.Enabled);
        Assert.Equal((BlendFactor.SrcAlpha, BlendFactor.OneMinusSrcAlpha), (state.Blend.RgbSource, state.Blend.RgbDestination));
        Assert.False(state.DepthWrite);
        Assert.True(MaterialDrawStates.From(Material(("gsys_render_state_mode", "translucent"), ("gsys_depth_test_write", "true"))).DepthWrite);
    }

    [Theory]
    [InlineData("opaque")]
    [InlineData("mask")]
    [InlineData("custom")]
    public void TheOtherModesWriteColourAsItIs(string mode)
    {
        DrawState state = MaterialDrawStates.From(Material(("gsys_render_state_mode", mode), ("gsys_render_state_blend_mode", "none")));
        Assert.Equal(BlendState.Off, state.Blend);
        Assert.True(state.DepthWrite);
    }

    [Fact]
    public void ALogicBlendModeSetsTheLogicOpInsteadOfBlending()
    {
        DrawState state = MaterialDrawStates.From(Material(("gsys_render_state_blend_mode", "logic"), ("gsys_logic_op", "xor")));
        Assert.False(state.Blend.Enabled);
        Assert.Equal(LogicOp.Xor, state.Raster.Logic);
    }

    [Fact]
    public void ThePriorityIsTheMaterialsOwnOrZero()
    {
        MaterialRecord with = new("Model", "model", 0, 0, [], [new RenderInfoRecord("gsys_priority", 0, [21], [], [])], [], [], [], ReadOnlyMemory<byte>.Empty, -1, -1);
        Assert.Equal(21, MaterialDrawStates.Priority(with));
        Assert.Equal(0, MaterialDrawStates.Priority(Material()));
    }
}
