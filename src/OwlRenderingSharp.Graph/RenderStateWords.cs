using OwlRenderingSharp.Content.Programs;
using OwlRenderingSharp.Gpu;

namespace OwlRenderingSharp.Graph;

/// <summary>The words render infos spell render state values with, and the values they mean.</summary>
/// <remarks>
/// Every value of <see cref="BlendFactor"/>, <see cref="BlendOp"/>, <see cref="DepthFunc"/>, <see cref="LogicOp"/>, <see cref="StencilOp"/>, <see cref="PolygonMode"/>
/// and <see cref="FrontFace"/> has a word, so a material that uses a value the game's own materials do not still draws as it asks. The game's materials use only a few of them
/// (add and max, one, src_alpha, one_minus_src_alpha, lequal). A word that is not in a list returns the fallback the caller gives. The words match the strings the engine
/// compares and the GX2 and NVN names, with the aliases the Wii U exporter wrote.
/// </remarks>
public static class RenderStateWords
{
    public static BlendOp Op(string? word, BlendOp fallback = BlendOp.Add) => word switch
    {
        "add" => BlendOp.Add,
        "sub" or "subtract" => BlendOp.Subtract,
        "rev_sub" or "reverse_sub" or "reverse_subtract" => BlendOp.ReverseSubtract,
        "min" => BlendOp.Min,
        "max" => BlendOp.Max,
        _ => fallback,
    };

    public static BlendFactor Factor(string? word, BlendFactor fallback) => word switch
    {
        "zero" => BlendFactor.Zero,
        "one" => BlendFactor.One,
        "src_color" => BlendFactor.SrcColor,
        "one_minus_src_color" => BlendFactor.OneMinusSrcColor,
        "dst_color" => BlendFactor.DstColor,
        "one_minus_dst_color" => BlendFactor.OneMinusDstColor,
        "src_alpha" => BlendFactor.SrcAlpha,
        "one_minus_src_alpha" => BlendFactor.OneMinusSrcAlpha,
        "dst_alpha" => BlendFactor.DstAlpha,
        "one_minus_dst_alpha" => BlendFactor.OneMinusDstAlpha,
        "const_color" or "constant_color" => BlendFactor.ConstantColor,
        "one_minus_const_color" or "one_minus_constant_color" => BlendFactor.OneMinusConstantColor,
        "const_alpha" or "constant_alpha" => BlendFactor.ConstantAlpha,
        "one_minus_const_alpha" or "one_minus_constant_alpha" => BlendFactor.OneMinusConstantAlpha,
        "src_alpha_saturate" => BlendFactor.SrcAlphaSaturate,
        "src1_color" => BlendFactor.Src1Color,
        "one_minus_src1_color" => BlendFactor.OneMinusSrc1Color,
        "src1_alpha" => BlendFactor.Src1Alpha,
        "one_minus_src1_alpha" => BlendFactor.OneMinusSrc1Alpha,
        _ => fallback,
    };

    public static DepthFunc Compare(string? word, DepthFunc fallback = DepthFunc.LessOrEqual) => word switch
    {
        "never" => DepthFunc.Never,
        "less" => DepthFunc.Less,
        "equal" => DepthFunc.Equal,
        "lequal" or "less_equal" => DepthFunc.LessOrEqual,
        "greater" => DepthFunc.Greater,
        "notequal" or "not_equal" => DepthFunc.NotEqual,
        "gequal" or "greater_equal" => DepthFunc.GreaterOrEqual,
        "always" => DepthFunc.Always,
        _ => fallback,
    };

    public static uint AlphaFunction(string? word) => word switch
    {
        "never" => GlslAlphaTest.Never,
        "less" => GlslAlphaTest.Less,
        "equal" => GlslAlphaTest.Equal,
        "lequal" or "less_equal" => GlslAlphaTest.LessOrEqual,
        "greater" => GlslAlphaTest.Greater,
        "notequal" or "not_equal" => GlslAlphaTest.NotEqual,
        "gequal" or "greater_equal" => GlslAlphaTest.GreaterOrEqual,
        "always" => GlslAlphaTest.Always,
        _ => GlslAlphaTest.Off,
    };

    public static LogicOp Logic(string? word, LogicOp fallback = LogicOp.Copy) => word switch
    {
        "clear" => LogicOp.Clear,
        "and" => LogicOp.And,
        "reverse_and" or "and_reverse" => LogicOp.AndReverse,
        "copy" => LogicOp.Copy,
        "inverse_and" or "and_inverted" => LogicOp.AndInverted,
        "no_operation" or "noop" => LogicOp.Noop,
        "xor" => LogicOp.Xor,
        "or" => LogicOp.Or,
        "nor" => LogicOp.Nor,
        "equivalent" or "equiv" => LogicOp.Equivalent,
        "inverse" or "invert" => LogicOp.Invert,
        "reverse_or" or "or_reverse" => LogicOp.OrReverse,
        "inverse_copy" or "copy_inverted" => LogicOp.CopyInverted,
        "inverse_or" or "or_inverted" => LogicOp.OrInverted,
        "nand" => LogicOp.Nand,
        "set" => LogicOp.Set,
        _ => fallback,
    };

    public static StencilOp Stencil(string? word, StencilOp fallback = StencilOp.Keep) => word switch
    {
        "keep" => StencilOp.Keep,
        "zero" => StencilOp.Zero,
        "replace" => StencilOp.Replace,
        "incr" or "increment_clamp" => StencilOp.IncrementClamp,
        "decr" or "decrement_clamp" => StencilOp.DecrementClamp,
        "invert" => StencilOp.Invert,
        "incr_wrap" or "increment_wrap" => StencilOp.IncrementWrap,
        "decr_wrap" or "decrement_wrap" => StencilOp.DecrementWrap,
        _ => fallback,
    };

    public static PolygonMode Polygon(string? word, PolygonMode fallback = PolygonMode.Fill) => word switch
    {
        "fill" or "tri" => PolygonMode.Fill,
        "line" or "wireframe" => PolygonMode.Line,
        "point" => PolygonMode.Point,
        _ => fallback,
    };

    public static FrontFace Winding(string? word, FrontFace fallback = FrontFace.CounterClockwise) => word switch
    {
        "ccw" or "counter_clockwise" => FrontFace.CounterClockwise,
        "cw" or "clockwise" => FrontFace.Clockwise,
        _ => fallback,
    };
}
