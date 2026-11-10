using Silk.NET.OpenGL;

namespace OwlRenderingSharp.Gpu;

/// <summary>A framebuffer over color textures and an optional depth texture, checked complete when it is created.</summary>
/// <remarks>
/// The attachments must all be the same size. An incomplete framebuffer throws a <see cref="GpuException"/> that names it and the GL status.
/// The framebuffer does not own its textures.
/// </remarks>
public sealed class GpuFramebuffer : IDisposable
{
    private readonly GpuDevice _device;
    private uint _handle;

    public GpuFramebuffer(GpuDevice device, string name, IReadOnlyList<GpuTexture> colors, GpuTexture? depth = null)
    {
        GpuTexture first = colors.FirstOrDefault() ?? depth ?? throw new GpuException("A framebuffer needs at least one attachment", resource: name);
        if (colors.Concat(depth is null ? [] : [depth]).Any(t => t.Width != first.Width || t.Height != first.Height))
            throw new GpuException("The attachments are not all the same size", resource: name);
        if (colors.Any(t => GpuFormats.IsDepth(t.Format)) || depth is not null && !GpuFormats.IsDepth(depth.Format))
            throw new GpuException("A depth format is attached as color, or a color format as depth", resource: name);

        _device = device;
        Name = name;
        Width = first.Width;
        Height = first.Height;
        GL gl = device.Gl;
        _handle = gl.CreateFramebuffer();
        GLEnum[] drawBuffers = new GLEnum[colors.Count];
        for (int i = 0; i < colors.Count; i++)
        {
            gl.NamedFramebufferTexture(_handle, FramebufferAttachment.ColorAttachment0 + i, colors[i].Handle, 0);
            drawBuffers[i] = GLEnum.ColorAttachment0 + i;
        }

        if (depth is not null) gl.NamedFramebufferTexture(_handle, GpuFormats.DepthAttachment(depth.Format), depth.Handle, 0);
        gl.NamedFramebufferDrawBuffers(_handle, (uint)drawBuffers.Length, drawBuffers);
        device.Check("GpuFramebuffer.Create", name);
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
