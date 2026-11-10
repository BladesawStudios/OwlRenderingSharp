using Silk.NET.OpenGL;

namespace OwlRenderingSharp.Gpu;

/// <summary>The GL swizzle value of a <see cref="GpuChannel"/>; a value that is not a channel reads as red, as BNTX tools do.</summary>
public static class GpuChannels
{
    public static int Enum(GpuChannel channel) => (int)(channel switch
    {
        GpuChannel.Zero => GLEnum.Zero,
        GpuChannel.One => GLEnum.One,
        GpuChannel.Green => GLEnum.Green,
        GpuChannel.Blue => GLEnum.Blue,
        GpuChannel.Alpha => GLEnum.Alpha,
        _ => GLEnum.Red,
    });
}
