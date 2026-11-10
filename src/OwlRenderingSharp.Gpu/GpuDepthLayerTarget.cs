using Silk.NET.OpenGL;

namespace OwlRenderingSharp.Gpu;

/// <summary>A depth-only framebuffer over one layer of a <see cref="GpuDepthArray"/>, checked complete when it is created.</summary>
/// <remarks>It has no colour attachment and reads and draws no colour. It does not own the array.</remarks>
public sealed class GpuDepthLayerTarget : IDisposable
{
    private readonly GpuDevice _device;
    private uint _handle;

    public GpuDepthLayerTarget(GpuDevice device, string name, GpuDepthArray array, int layer)
    {
        if (layer < 0 || layer >= array.Layers) throw new GpuException($"Layer {layer} does not exist; the array has {array.Layers}", resource: name);
        _device = device;
        Name = name;
        Width = array.Width;
        Height = array.Height;
        GL gl = device.Gl;
        _handle = gl.CreateFramebuffer();
        gl.NamedFramebufferTextureLayer(_handle, FramebufferAttachment.DepthAttachment, array.Handle, 0, layer);
        gl.NamedFramebufferDrawBuffer(_handle, ColorBuffer.None);
        gl.NamedFramebufferReadBuffer(_handle, ColorBuffer.None);
        device.Check("GpuDepthLayerTarget.Create", name);
        GLEnum status = gl.CheckNamedFramebufferStatus(_handle, FramebufferTarget.Framebuffer);
        if (status != GLEnum.FramebufferComplete) throw new GpuException($"The framebuffer is not complete: {status}", resource: name);
    }

    public string Name { get; }

    public int Width { get; }

    public int Height { get; }

    public uint Handle => _handle;

    public void Dispose()
    {
        if (_handle == 0) return;
        _device.Gl.DeleteFramebuffer(_handle);
        _handle = 0;
    }
}
