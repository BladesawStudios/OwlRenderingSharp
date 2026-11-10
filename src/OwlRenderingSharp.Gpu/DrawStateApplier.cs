using Silk.NET.OpenGL;

namespace OwlRenderingSharp.Gpu;

/// <summary>Sets every piece of a <see cref="DrawState"/> on a GL context.</summary>
/// <remarks>
/// <see cref="GpuDevice.Apply"/> owns one. Each piece is set whether or not it changed, so the context never depends on what an earlier draw left behind. A piece that is off
/// is switched off and its other settings are not touched.
/// </remarks>
internal sealed class DrawStateApplier(GL gl)
{
    public void Apply(DrawState state)
    {
        ApplyDepth(state);
        ApplyCull(state);
        gl.ColorMask(state.Channels.HasFlag(ColorMask.Red), state.Channels.HasFlag(ColorMask.Green), state.Channels.HasFlag(ColorMask.Blue), state.Channels.HasFlag(ColorMask.Alpha));
        ApplyBlend(state.Blend);
        ApplyRaster(state.Raster);
        ApplyStencil(state.Stencil);
    }

    private void ApplyDepth(DrawState state)
    {
        Set(EnableCap.DepthTest, state.DepthTest);
        gl.DepthMask(state.DepthWrite);
        gl.DepthFunc(GlStateMaps.Compare(state.DepthFunc));
    }

    private void ApplyCull(DrawState state)
    {
        Set(EnableCap.CullFace, state.Cull != CullMode.None);
        if (state.Cull != CullMode.None) gl.CullFace(state.Cull switch { CullMode.Front => TriangleFace.Front, CullMode.Both => TriangleFace.FrontAndBack, _ => TriangleFace.Back });
    }

    private void ApplyBlend(BlendState blend)
    {
        Set(EnableCap.Blend, blend.Enabled);
        if (!blend.Enabled) return;
        gl.BlendEquationSeparate((BlendEquationModeEXT)GlStateMaps.Equation(blend.RgbOp), (BlendEquationModeEXT)GlStateMaps.Equation(blend.AlphaOp));
        gl.BlendFuncSeparate(
            (BlendingFactor)GlStateMaps.Factor(blend.RgbSource),
            (BlendingFactor)GlStateMaps.Factor(blend.RgbDestination),
            (BlendingFactor)GlStateMaps.Factor(blend.AlphaSource),
            (BlendingFactor)GlStateMaps.Factor(blend.AlphaDestination));
        gl.BlendColor(blend.Constant.X, blend.Constant.Y, blend.Constant.Z, blend.Constant.W);
    }

    private void ApplyRaster(RasterState raster)
    {
        gl.PolygonMode(GLEnum.FrontAndBack, GlStateMaps.Polygon(raster.Polygon));
        gl.FrontFace(raster.Front == FrontFace.Clockwise ? GLEnum.CW : GLEnum.Ccw);
        bool bias = raster.DepthBiasSlope != 0f || raster.DepthBiasUnits != 0f;
        Set(EnableCap.PolygonOffsetFill, bias);
        Set(EnableCap.PolygonOffsetLine, bias);
        Set(EnableCap.PolygonOffsetPoint, bias);
        if (bias) gl.PolygonOffset(raster.DepthBiasSlope, raster.DepthBiasUnits);
        Set(EnableCap.SampleAlphaToCoverage, raster.AlphaToCoverage);
        Set(EnableCap.ColorLogicOp, raster.Logic != LogicOp.None);
        if (raster.Logic != LogicOp.None) gl.LogicOp(GlStateMaps.Logic(raster.Logic));
    }

    private void ApplyStencil(StencilState stencil)
    {
        Set(EnableCap.StencilTest, stencil.Enabled);
        if (!stencil.Enabled) return;
        Face(TriangleFace.Front, stencil.Front);
        Face(TriangleFace.Back, stencil.Back);
    }

    private void Face(TriangleFace face, StencilFace stencil)
    {
        gl.StencilFuncSeparate(face, (StencilFunction)GlStateMaps.Compare(stencil.Func), stencil.Reference, stencil.Mask);
        gl.StencilMaskSeparate(face, stencil.WriteMask);
        gl.StencilOpSeparate(face, (GLEnum)GlStateMaps.Stencil(stencil.Fail), (GLEnum)GlStateMaps.Stencil(stencil.DepthFail), (GLEnum)GlStateMaps.Stencil(stencil.Pass));
    }

    private void Set(EnableCap cap, bool on)
    {
        if (on) gl.Enable(cap);
        else gl.Disable(cap);
    }
}
