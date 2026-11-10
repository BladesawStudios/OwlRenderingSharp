using Silk.NET.OpenGL;

namespace OwlRenderingSharp.Gpu;

/// <summary>A cube map for a stand-in environment probe: 1x1 faces of one colour each, or square float faces with a full mip chain.</summary>
/// <remarks>
/// The six colours are in the order +X, -X, +Y, -Y, +Z, -Z, each packed red in the low byte to alpha in the high byte. Texture reads blend across faces at the edges, so a
/// sky above and a ground below give a smooth gradient. The float form takes four floats per texel for each face, stores them in sixteen-bit float and builds the mips; <see cref="Levels"/> is the count. Created with direct state access; it does not own anything but its texture.
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

    /// <summary>A cube of <paramref name="size"/> square float faces (+X, -X, +Y, -Y, +Z, -Z, each size x size x 4 floats, top row first) with all its mip levels.</summary>
    public GpuCubeTexture(GpuDevice device, string name, int size, IReadOnlyList<float[]> faces)
    {
        if (faces.Count != 6) throw new GpuException($"A cube map takes six faces and {faces.Count} were given", resource: name);
        if (faces.Any(f => f.Length != size * size * 4)) throw new GpuException($"Each face of a cube of {size} needs {size * size * 4} floats", resource: name);
        _device = device;
        Name = name;
        Levels = (int)Math.Log2(size) + 1;
        _handle = device.Gl.CreateTexture(TextureTarget.TextureCubeMap);
        device.Gl.TextureStorage2D(_handle, (uint)Levels, SizedInternalFormat.Rgba16f, (uint)size, (uint)size);
        for (int face = 0; face < 6; face++)
            device.Gl.TextureSubImage3D(_handle, 0, 0, 0, face, (uint)size, (uint)size, 1, PixelFormat.Rgba, PixelType.Float, (ReadOnlySpan<float>)faces[face]);
        device.Gl.GenerateTextureMipmap(_handle);
        device.Check("GpuCubeTexture.Create", name);
    }

    public string Name { get; }

    public int Levels { get; } = 1;

    public uint Handle => _handle;

    public void Dispose()
    {
        if (_handle == 0) return;
        _device.Gl.DeleteTexture(_handle);
        _handle = 0;
    }
}
