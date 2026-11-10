using Silk.NET.OpenGL;

namespace OwlRenderingSharp.Gpu;

/// <summary>The pure translation from the library's render state enums to the GL enums that set them.</summary>
/// <remarks>It holds no state. Each map is total: every value of its enum has an entry, which the tests check.</remarks>
public static class GlStateMaps
{
    public static GLEnum Compare(DepthFunc func) => func switch
    {
        DepthFunc.Never => GLEnum.Never,
        DepthFunc.Less => GLEnum.Less,
        DepthFunc.Equal => GLEnum.Equal,
        DepthFunc.LessOrEqual => GLEnum.Lequal,
        DepthFunc.Greater => GLEnum.Greater,
        DepthFunc.NotEqual => GLEnum.Notequal,
        DepthFunc.GreaterOrEqual => GLEnum.Gequal,
        _ => GLEnum.Always,
    };

    public static GLEnum Equation(BlendOp op) => op switch
    {
        BlendOp.Subtract => GLEnum.FuncSubtract,
        BlendOp.ReverseSubtract => GLEnum.FuncReverseSubtract,
        BlendOp.Min => GLEnum.Min,
        BlendOp.Max => GLEnum.Max,
        _ => GLEnum.FuncAdd,
    };

    public static GLEnum Factor(BlendFactor factor) => factor switch
    {
        BlendFactor.Zero => GLEnum.Zero,
        BlendFactor.SrcColor => GLEnum.SrcColor,
        BlendFactor.OneMinusSrcColor => GLEnum.OneMinusSrcColor,
        BlendFactor.DstColor => GLEnum.DstColor,
        BlendFactor.OneMinusDstColor => GLEnum.OneMinusDstColor,
        BlendFactor.SrcAlpha => GLEnum.SrcAlpha,
        BlendFactor.OneMinusSrcAlpha => GLEnum.OneMinusSrcAlpha,
        BlendFactor.DstAlpha => GLEnum.DstAlpha,
        BlendFactor.OneMinusDstAlpha => GLEnum.OneMinusDstAlpha,
        BlendFactor.ConstantColor => GLEnum.ConstantColor,
        BlendFactor.OneMinusConstantColor => GLEnum.OneMinusConstantColor,
        BlendFactor.ConstantAlpha => GLEnum.ConstantAlpha,
        BlendFactor.OneMinusConstantAlpha => GLEnum.OneMinusConstantAlpha,
        BlendFactor.SrcAlphaSaturate => GLEnum.SrcAlphaSaturate,
        BlendFactor.Src1Color => GLEnum.Src1Color,
        BlendFactor.OneMinusSrc1Color => GLEnum.OneMinusSrc1Color,
        BlendFactor.Src1Alpha => GLEnum.Src1Alpha,
        BlendFactor.OneMinusSrc1Alpha => GLEnum.OneMinusSrc1Alpha,
        _ => GLEnum.One,
    };

    public static GLEnum Logic(LogicOp op) => op switch
    {
        LogicOp.Clear => GLEnum.Clear,
        LogicOp.And => GLEnum.And,
        LogicOp.AndReverse => GLEnum.AndReverse,
        LogicOp.AndInverted => GLEnum.AndInverted,
        LogicOp.Noop => GLEnum.Noop,
        LogicOp.Xor => GLEnum.Xor,
        LogicOp.Or => GLEnum.Or,
        LogicOp.Nor => GLEnum.Nor,
        LogicOp.Equivalent => GLEnum.Equiv,
        LogicOp.Invert => GLEnum.Invert,
        LogicOp.OrReverse => GLEnum.OrReverse,
        LogicOp.CopyInverted => GLEnum.CopyInverted,
        LogicOp.OrInverted => GLEnum.OrInverted,
        LogicOp.Nand => GLEnum.Nand,
        LogicOp.Set => GLEnum.Set,
        _ => GLEnum.Copy,
    };

    public static GLEnum Stencil(StencilOp op) => op switch
    {
        StencilOp.Zero => GLEnum.Zero,
        StencilOp.Replace => GLEnum.Replace,
        StencilOp.IncrementClamp => GLEnum.Incr,
        StencilOp.DecrementClamp => GLEnum.Decr,
        StencilOp.Invert => GLEnum.Invert,
        StencilOp.IncrementWrap => GLEnum.IncrWrap,
        StencilOp.DecrementWrap => GLEnum.DecrWrap,
        _ => GLEnum.Keep,
    };

    public static GLEnum Polygon(PolygonMode mode) => mode switch
    {
        PolygonMode.Line => GLEnum.Line,
        PolygonMode.Point => GLEnum.Point,
        _ => GLEnum.Fill,
    };
}
