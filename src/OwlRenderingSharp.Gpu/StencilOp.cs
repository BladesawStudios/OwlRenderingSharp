namespace OwlRenderingSharp.Gpu;

/// <summary>What happens to the stencil value when a stencil or depth test passes or fails.</summary>
public enum StencilOp
{
    Keep,
    Zero,
    Replace,
    IncrementClamp,
    DecrementClamp,
    Invert,
    IncrementWrap,
    DecrementWrap,
}
