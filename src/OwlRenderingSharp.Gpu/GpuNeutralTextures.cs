using Silk.NET.OpenGL;

namespace OwlRenderingSharp.Gpu;

/// <summary>One-texel textures of each sampler kind and colour, white unless asked otherwise,, for the samplers a program declares that the renderer has nothing real to bind to.</summary>
/// <remarks>
/// A program samples every texture its code mentions whether or not the draw provides it, and an unbound unit reads black or is invalid.
/// A neutral texture makes the read well-defined. Each is created on first use, and cubes and 3D textures have all their faces and layers filled.
/// </remarks>
public sealed class GpuNeutralTextures(GpuDevice device) : IDisposable
{
    private readonly Dictionary<(GpuSamplerKind, uint), uint> _handles = [];

    public uint Handle(GpuSamplerKind kind) => Handle(kind, 0xFFFFFFFF);

    // The colour is packed as red in the low byte to alpha in the high byte.
    public uint Handle(GpuSamplerKind kind, uint rgba)
    {
        if (!_handles.TryGetValue((kind, rgba), out uint handle)) _handles[(kind, rgba)] = handle = Create(kind, rgba);
        return handle;
    }

    public void Dispose()
    {
        foreach (uint handle in _handles.Values) device.Gl.DeleteTexture(handle);
        _handles.Clear();
    }

    private unsafe uint Create(GpuSamplerKind kind, uint rgba)
    {
        GL gl = device.Gl;
        TextureTarget target = kind switch
        {
            GpuSamplerKind.TextureCube => TextureTarget.TextureCubeMap,
            GpuSamplerKind.Texture3D => TextureTarget.Texture3D,
            GpuSamplerKind.Texture2DArray => TextureTarget.Texture2DArray,
            _ => TextureTarget.Texture2D,
        };
        uint handle = gl.CreateTexture(target);
        if (target is TextureTarget.Texture3D or TextureTarget.Texture2DArray) gl.TextureStorage3D(handle, 1, SizedInternalFormat.Rgba8, 1, 1, 1);
        else gl.TextureStorage2D(handle, 1, SizedInternalFormat.Rgba8, 1, 1);
        byte[] color = [(byte)rgba, (byte)(rgba >> 8), (byte)(rgba >> 16), (byte)(rgba >> 24)];
        fixed (byte* pixel = color) gl.ClearTexImage(handle, 0, PixelFormat.Rgba, PixelType.UnsignedByte, pixel);
        device.Check("GpuNeutralTextures.Create", kind.ToString());
        return handle;
    }
}
