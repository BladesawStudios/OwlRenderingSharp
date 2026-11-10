using Silk.NET.OpenGL;

namespace OwlRenderingSharp.Gpu;

/// <summary>A 2D texture with immutable storage, created with direct state access.</summary>
/// <remarks>
/// The format, size and mip count are fixed at creation. Pixels are uploaded as spans of unmanaged values in the format's own pixel
/// layout, and a level that does not exist or an upload that does not match its size throws a <see cref="GpuException"/> naming the texture.
/// Mips are generated on request, not on upload.
/// </remarks>
public sealed unsafe class GpuTexture : IDisposable
{
    private readonly GpuDevice _device;
    private uint _handle;

    public GpuTexture(GpuDevice device, string name, GpuFormat format, int width, int height, int mipCount = 1)
    {
        if (width <= 0 || height <= 0 || mipCount <= 0) throw new GpuException($"A texture of {width}x{height} with {mipCount} mips cannot be created", resource: name);
        _device = device;
        Name = name;
        Format = format;
        Width = width;
        Height = height;
        MipCount = mipCount;
        _handle = device.Gl.CreateTexture(TextureTarget.Texture2D);
        device.Gl.TextureStorage2D(_handle, (uint)mipCount, GpuFormats.Internal(format), (uint)width, (uint)height);
        device.Check("GpuTexture.Create", name);
    }

    public string Name { get; }

    public GpuFormat Format { get; }

    public int Width { get; }

    public int Height { get; }

    public int MipCount { get; }

    public uint Handle => _handle;

    public void Upload<T>(ReadOnlySpan<T> pixels, int level = 0) where T : unmanaged
    {
        (int w, int h) = LevelSize(level);
        if (GpuFormats.IsCompressed(Format)) throw new GpuException("A block-compressed texture is uploaded with UploadCompressed", resource: Name);
        int expected = w * h * PixelBytes(Format);
        int actual = pixels.Length * System.Runtime.CompilerServices.Unsafe.SizeOf<T>();
        if (actual != expected) throw new GpuException($"Level {level} takes {expected} bytes and {actual} were given", resource: Name);
        _device.Gl.TextureSubImage2D(_handle, level, 0, 0, (uint)w, (uint)h, GpuFormats.Pixel(Format), GpuFormats.Type(Format), pixels);
        _device.Check("GpuTexture.Upload", Name);
    }

    public void UploadCompressed(ReadOnlySpan<byte> blocks, int level = 0)
    {
        (int w, int h) = LevelSize(level);
        int expected = LevelBytes(w, h);
        if (blocks.Length != expected) throw new GpuException($"Level {level} takes {expected} bytes and {blocks.Length} were given", resource: Name);
        fixed (byte* data = blocks) _device.Gl.CompressedTextureSubImage2D(_handle, level, 0, 0, (uint)w, (uint)h, (GLEnum)GpuFormats.Internal(Format), (uint)blocks.Length, data);
        _device.Check("GpuTexture.UploadCompressed", Name);
    }

    public T[] Download<T>(int level = 0) where T : unmanaged
    {
        (int w, int h) = LevelSize(level);
        int bytes = LevelBytes(w, h);
        T[] pixels = new T[bytes / System.Runtime.CompilerServices.Unsafe.SizeOf<T>()];
        if (GpuFormats.IsCompressed(Format)) _device.Gl.GetCompressedTextureImage(_handle, level, (uint)bytes, new Span<T>(pixels));
        else _device.Gl.GetTextureImage(_handle, level, GpuFormats.Pixel(Format), GpuFormats.Type(Format), (uint)bytes, new Span<T>(pixels));
        _device.Check("GpuTexture.Download", Name);
        return pixels;
    }

    /// <summary>The base level as four 32-bit floats per pixel, whatever the stored format, converted by GL; for float and normalized colour formats.</summary>
    public float[] DownloadAsFloats()
    {
        if (GpuFormats.IsCompressed(Format) || GpuFormats.IsDepth(Format)) throw new GpuException("Only an uncompressed colour texture is read back as floats", resource: Name);
        float[] pixels = new float[Width * Height * 4];
        _device.Gl.GetTextureImage(_handle, 0, PixelFormat.Rgba, PixelType.Float, (uint)(pixels.Length * sizeof(float)), new Span<float>(pixels));
        _device.Check("GpuTexture.DownloadAsFloats", Name);
        return pixels;
    }

    /// <summary>Makes a one-channel texture read as (r, r, r, 1), the way the console samples one, instead of GL's (r, 0, 0, 1).</summary>
    public void ReplicateRed()
    {
        _device.Gl.TextureParameter(_handle, TextureParameterName.TextureSwizzleG, (int)GLEnum.Red);
        _device.Gl.TextureParameter(_handle, TextureParameterName.TextureSwizzleB, (int)GLEnum.Red);
        _device.Gl.TextureParameter(_handle, TextureParameterName.TextureSwizzleA, (int)GLEnum.One);
        _device.Check("GpuTexture.ReplicateRed", Name);
    }

    public void GenerateMips()
    {
        _device.Gl.GenerateTextureMipmap(_handle);
        _device.Check("GpuTexture.GenerateMips", Name);
    }

    public void Dispose()
    {
        if (_handle == 0) return;
        _device.Gl.DeleteTexture(_handle);
        _handle = 0;
    }

    public static int PixelBytes(GpuFormat format) => format switch
    {
        GpuFormat.R8 => 1,
        GpuFormat.R16F => 2,
        GpuFormat.Rgba8 or GpuFormat.Rgba8Srgb or GpuFormat.Rg16F or GpuFormat.R32F or GpuFormat.R32Uint or GpuFormat.Depth32F or GpuFormat.Depth24Stencil8 => 4,
        GpuFormat.Rgba16F => 8,
        GpuFormat.Rgba32F => 16,
        _ => throw new ArgumentOutOfRangeException(nameof(format)),
    };

    private int LevelBytes(int width, int height) => GpuFormats.IsCompressed(Format)
        ? (width + 3) / 4 * ((height + 3) / 4) * GpuFormats.BlockBytes(Format)
        : width * height * PixelBytes(Format);

    private (int Width, int Height) LevelSize(int level)
    {
        if (level < 0 || level >= MipCount) throw new GpuException($"Level {level} does not exist; the texture has {MipCount}", resource: Name);
        return (Math.Max(1, Width >> level), Math.Max(1, Height >> level));
    }
}
