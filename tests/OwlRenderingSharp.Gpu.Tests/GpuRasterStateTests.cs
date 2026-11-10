using Silk.NET.OpenGL;

namespace OwlRenderingSharp.Gpu.Tests;

[Collection(GlCollection.Name)]
public class GpuRasterStateTests(GlFixture gl)
{
    [Fact]
    public void EveryEnumValueMapsToAGlValue()
    {
        foreach (StencilOp o in Enum.GetValues<StencilOp>()) _ = GlStateMaps.Stencil(o);
        Assert.Equal(Enum.GetValues<StencilOp>().Length, Enum.GetValues<StencilOp>().Select(GlStateMaps.Stencil).Distinct().Count());
        Assert.Equal(Enum.GetValues<BlendFactor>().Length, Enum.GetValues<BlendFactor>().Select(GlStateMaps.Factor).Distinct().Count());
        Assert.Equal(Enum.GetValues<LogicOp>().Length - 1, Enum.GetValues<LogicOp>().Where(o => o != LogicOp.None).Select(GlStateMaps.Logic).Distinct().Count());
    }

    [SkippableFact]
    public void DualSourceFactorsAndSaturateAreAccepted()
    {
        gl.Require().Run(device =>
        {
            BlendState blend = BlendState.Off with { Enabled = true, RgbSource = BlendFactor.Src1Color, RgbDestination = BlendFactor.OneMinusSrc1Alpha, AlphaSource = BlendFactor.SrcAlphaSaturate };
            device.Apply(DrawState.Opaque with { Blend = blend });
            device.Check();
            Assert.Equal((int)GLEnum.Src1Color, device.Gl.GetInteger(GetPName.BlendSrcRgb));
            Assert.Equal((int)GLEnum.OneMinusSrc1Alpha, device.Gl.GetInteger(GetPName.BlendDstRgb));
            Assert.Equal((int)GLEnum.SrcAlphaSaturate, device.Gl.GetInteger(GetPName.BlendSrcAlpha));
            device.Apply(DrawState.Opaque);
        });
    }

    [SkippableFact]
    public void ALogicOpTurnsTheColourLogicOpOnAndOffAgain()
    {
        gl.Require().Run(device =>
        {
            device.Apply(DrawState.Opaque with { Raster = new RasterState(PolygonMode.Fill, FrontFace.CounterClockwise, 0, 0, false, LogicOp.Xor) });
            Assert.True(device.Gl.IsEnabled(EnableCap.ColorLogicOp));
            Assert.Equal((int)GLEnum.Xor, device.Gl.GetInteger(GetPName.LogicOpMode));
            device.Apply(DrawState.Opaque);
            Assert.False(device.Gl.IsEnabled(EnableCap.ColorLogicOp));
        });
    }

    [SkippableFact]
    public void PolygonModeWindingBiasAndAlphaToCoverageAreSet()
    {
        gl.Require().Run(device =>
        {
            device.Apply(DrawState.Opaque with { Raster = new RasterState(PolygonMode.Line, FrontFace.Clockwise, 1.5f, 2f, true, LogicOp.None) });
            device.Check();
            Assert.Equal((int)GLEnum.CW, device.Gl.GetInteger(GetPName.FrontFace));
            Assert.True(device.Gl.IsEnabled(EnableCap.PolygonOffsetFill));
            Assert.Equal(1.5f, device.Gl.GetFloat(GetPName.PolygonOffsetFactor));
            Assert.Equal(2f, device.Gl.GetFloat(GetPName.PolygonOffsetUnits));
            Assert.True(device.Gl.IsEnabled(EnableCap.SampleAlphaToCoverage));
            device.Apply(DrawState.Opaque);
            Assert.False(device.Gl.IsEnabled(EnableCap.PolygonOffsetFill));
            Assert.Equal((int)GLEnum.Ccw, device.Gl.GetInteger(GetPName.FrontFace));
        });
    }

    [SkippableFact]
    public void TheStencilTestIsSetPerFace()
    {
        gl.Require().Run(device =>
        {
            StencilFace front = new(DepthFunc.Equal, 3, 0x0F, 0xF0, StencilOp.Zero, StencilOp.Invert, StencilOp.IncrementWrap);
            StencilFace back = StencilFace.Passthrough with { Pass = StencilOp.Replace };
            device.Apply(DrawState.Opaque with { Stencil = new StencilState(true, front, back) });
            device.Check();
            Assert.True(device.Gl.IsEnabled(EnableCap.StencilTest));
            Assert.Equal((int)GLEnum.Equal, device.Gl.GetInteger(GetPName.StencilFunc));
            Assert.Equal(3, device.Gl.GetInteger(GetPName.StencilRef));
            Assert.Equal((int)GLEnum.IncrWrap, device.Gl.GetInteger(GetPName.StencilPassDepthPass));
            Assert.Equal((int)GLEnum.Replace, device.Gl.GetInteger(GetPName.StencilBackPassDepthPass));
            device.Apply(DrawState.Opaque);
            Assert.False(device.Gl.IsEnabled(EnableCap.StencilTest));
        });
    }

    [SkippableFact]
    public void EachColourChannelCanBeMaskedAlone()
    {
        gl.Require().Run(device =>
        {
            device.Apply(DrawState.Opaque with { Channels = ColorMask.Red | ColorMask.Alpha });
            bool[] mask = new bool[4];
            device.Gl.GetBoolean(GetPName.ColorWritemask, mask);
            Assert.Equal([true, false, false, true], mask);
            device.Apply(DrawState.Opaque);
        });
    }
}
