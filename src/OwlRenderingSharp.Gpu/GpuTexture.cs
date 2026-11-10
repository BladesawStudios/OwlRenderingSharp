using Silk.NET.OpenGL;

namespace OwlRenderingSharp.Gpu;

/// <summary>A 2D texture with immutable storage, created with direct state access.</summary>
/// <remarks>
/// The format, size and mip count are fixed at creation. Pixels are uploaded as spans of unmanaged values in the format's own pixel
/// layout, and a level that does not exist or an upload that does not match its size throws a <see cref="GpuException"/> naming the texture.
/// Mips are generated on request, not on upload.
/// </remarks>
public sealed class GpuTexture : IDisposable
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
        int expected = w * h * PixelBytes(Format);
        int actual = pixels.Length * System.Runtime.CompilerServices.Unsafe.SizeOf<T>();
        if (actual != expected) throw new GpuException($"Level {level} takes {expected} bytes and {actual} were given", resource: Name);
        _device.Gl.TextureSubImage2D(_handle, level, 0, 0, (uint)w, (uint)h, GpuFormats.Pixel(Format), GpuFormats.Type(Format), pixels);
        _device.Check("GpuTexture.Upload", Name);
    }

    public T[] Download<T>(int level = 0) where T : unmanaged
    {
        (int w, int h) = LevelSize(level);
        int bytes = w * h * PixelBytes(Format);
        T[] pixels = new T[bytes / System.Runtime.CompilerServices.Unsafe.SizeOf<T>()];
        _device.Gl.GetTextureImage(_handle, level, GpuFormats.Pixel(Format), GpuFormats.Type(Format), (uint)bytes, new Span<T>(pixels));
        _device.Check("GpuTexture.Download", Name);
        return pixels;
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

    private (int Width, int Height) LevelSize(int level)
    {
        if (level < 0 || level >= MipCount) throw new GpuException($"Level {level} does not exist; the texture has {MipCount}", resource: Name);
        return (Math.Max(1, Width >> level), Math.Max(1, Height >> level));
    }
}
