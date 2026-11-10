using System.Numerics;
using Silk.NET.OpenGL;

namespace OwlRenderingSharp.Gpu.Tests;

[Collection(GlCollection.Name)]
public class GpuBlendTests(GlFixture gl)
{
    private static BlendState AlphaBlend => new(true, BlendOp.Add, BlendFactor.SrcAlpha, BlendFactor.OneMinusSrcAlpha, BlendOp.Add, BlendFactor.One, BlendFactor.One, new Vector4(0.25f, 0.5f, 0.75f, 1f));

    [SkippableFact]
    public void ABlendingStateSetsTheEquationAndFactors()
    {
        gl.Require().Run(device =>
        {
            device.Apply(DrawState.Opaque with { Blend = AlphaBlend });
            Assert.True(device.Gl.IsEnabled(EnableCap.Blend));
            Assert.Equal((int)BlendingFactor.SrcAlpha, device.Gl.GetInteger(GetPName.BlendSrcRgb));
            Assert.Equal((int)BlendingFactor.OneMinusSrcAlpha, device.Gl.GetInteger(GetPName.BlendDstRgb));
            Assert.Equal((int)BlendingFactor.One, device.Gl.GetInteger(GetPName.BlendDstAlpha));
        });
    }

    [SkippableFact]
    public void AnOpaqueStateTurnsBlendingOffAndColourWritesOnOrOff()
    {
        gl.Require().Run(device =>
        {
            device.Apply(DrawState.Opaque with { Blend = AlphaBlend, ColorWrite = false });
            device.Apply(DrawState.Opaque);
            Assert.False(device.Gl.IsEnabled(EnableCap.Blend));
            bool[] mask = new bool[4];
            device.Gl.GetBoolean(GetPName.ColorWritemask, mask);
            Assert.All(mask, on => Assert.True(on));
            device.Apply(DrawState.Opaque with { ColorWrite = false });
            device.Gl.GetBoolean(GetPName.ColorWritemask, mask);
            Assert.All(mask, on => Assert.False(on));
        });
    }
}
