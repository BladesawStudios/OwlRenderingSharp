using Silk.NET.OpenGL;

namespace OwlRenderingSharp.Gpu;

/// <summary>A cube map of 1x1 faces, each one colour, for a stand-in environment probe.</summary>
/// <remarks>
/// The six colours are in the order +X, -X, +Y, -Y, +Z, -Z, each packed red in the low byte to alpha in the high byte. Texture reads blend across faces at the edges, so a
/// sky above and a ground below give a smooth gradient. Created with direct state access; it does not own anything but its texture.
/// </remarks>
public sealed class GpuCubeTexture : IDisposable
{
    private readonly GpuDevice _device;
    private uint _handle;

    public GpuCubeTexture(GpuDevice device, string name, IReadOnlyList<uint> faces)
    {
        if (faces.Count != 6) throw new GpuException($"A cube map takes six faces and {faces.Count} were given", resource: name);
        _device = device;
        Name = name;
        _handle = device.Gl.CreateTexture(TextureTarget.TextureCubeMap);
        device.Gl.TextureStorage2D(_handle, 1, SizedInternalFormat.Rgba8, 1, 1);
        for (int face = 0; face < 6; face++)
        {
            byte[] pixel = [(byte)faces[face], (byte)(faces[face] >> 8), (byte)(faces[face] >> 16), (byte)(faces[face] >> 24)];
            device.Gl.TextureSubImage3D(_handle, 0, 0, 0, face, 1, 1, 1, PixelFormat.Rgba, PixelType.UnsignedByte, (ReadOnlySpan<byte>)pixel);
        }

        device.Check("GpuCubeTexture.Create", name);
    }

    public string Name { get; }

    public uint Handle => _handle;

    public void Dispose()
    {
        if (_handle == 0) return;
        _device.Gl.DeleteTexture(_handle);
        _handle = 0;
    }
}
