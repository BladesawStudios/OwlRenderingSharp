using OwlRenderingSharp.Gpu;

namespace OwlRenderingSharp.Graph.Tests;

public class RenderStateWordsTests
{
    private static readonly string[] BlendOps = ["add", "sub", "rev_sub", "min", "max"];

    private static readonly string[] BlendFactors =
    [
        "zero", "one", "src_color", "one_minus_src_color", "dst_color", "one_minus_dst_color", "src_alpha", "one_minus_src_alpha", "dst_alpha", "one_minus_dst_alpha",
        "const_color", "one_minus_const_color", "const_alpha", "one_minus_const_alpha", "src_alpha_saturate", "src1_color", "one_minus_src1_color", "src1_alpha", "one_minus_src1_alpha",
    ];

    private static readonly string[] Compares = ["never", "less", "equal", "lequal", "greater", "notequal", "gequal", "always"];

    private static readonly string[] Logics =
    [
        "clear", "and", "reverse_and", "copy", "inverse_and", "no_operation", "xor", "or", "nor", "equivalent", "inverse", "reverse_or", "inverse_copy", "inverse_or", "nand", "set",
    ];

    private static readonly string[] Stencils = ["keep", "zero", "replace", "incr", "decr", "invert", "incr_wrap", "decr_wrap"];

    [Fact]
    public void EveryBlendOpHasAWord()
    {
        Assert.Equal(Enum.GetValues<BlendOp>().Order(), BlendOps.Select(w => RenderStateWords.Op(w)).Order());
    }

    [Fact]
    public void EveryBlendFactorHasAWord()
    {
        Assert.Equal(Enum.GetValues<BlendFactor>().Order(), BlendFactors.Select(w => RenderStateWords.Factor(w, BlendFactor.Zero)).Order());
    }

    [Fact]
    public void EveryCompareFunctionHasAWord()
    {
        Assert.Equal(Enum.GetValues<DepthFunc>().Order(), Compares.Select(w => RenderStateWords.Compare(w)).Order());
    }

    [Fact]
    public void EveryLogicOpExceptNoneHasAWord()
    {
        Assert.Equal(Enum.GetValues<LogicOp>().Where(o => o != LogicOp.None).Order(), Logics.Select(w => RenderStateWords.Logic(w)).Order());
    }

    [Fact]
    public void EveryStencilOpHasAWord()
    {
        Assert.Equal(Enum.GetValues<StencilOp>().Order(), Stencils.Select(w => RenderStateWords.Stencil(w)).Order());
    }

    [Fact]
    public void PolygonModesAndWindingsHaveWords()
    {
        Assert.Equal([PolygonMode.Fill, PolygonMode.Line, PolygonMode.Point], new[] { "fill", "line", "point" }.Select(w => RenderStateWords.Polygon(w)));
        Assert.Equal([FrontFace.CounterClockwise, FrontFace.Clockwise], new[] { "ccw", "cw" }.Select(w => RenderStateWords.Winding(w)));
    }

    [Fact]
    public void AnUnknownWordKeepsTheCallersFallback()
    {
        Assert.Equal(BlendFactor.DstColor, RenderStateWords.Factor("nonsense", BlendFactor.DstColor));
        Assert.Equal(BlendOp.Max, RenderStateWords.Op(null, BlendOp.Max));
    }
}
