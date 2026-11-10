namespace OwlRenderingSharp.Gpu;

/// <summary>The stencil test and update of one face orientation.</summary>
/// <remarks>The compare function reuses <see cref="DepthFunc"/>, which names the eight comparisons.</remarks>
public readonly record struct StencilFace(DepthFunc Func, int Reference, uint Mask, uint WriteMask, StencilOp Fail, StencilOp DepthFail, StencilOp Pass)
{
    public static StencilFace Passthrough { get; } = new(DepthFunc.Always, 0, 0xFF, 0xFF, StencilOp.Keep, StencilOp.Keep, StencilOp.Keep);
}
