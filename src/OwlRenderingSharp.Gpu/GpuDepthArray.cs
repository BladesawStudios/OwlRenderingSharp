using Silk.NET.OpenGL;

namespace OwlRenderingSharp.Gpu;

/// <summary>A 2D array texture of 32-bit float depth, one layer per shadow cascade, with immutable storage.</summary>
/// <remarks>
/// Each layer is rendered through its own <see cref="GpuDepthLayerTarget"/> and the whole array is sampled with a depth-comparison sampler. It is created with direct state access.
/// </remarks>
public sealed class GpuDepthArray : IDisposable
{
    private readonly GpuDevice _device;
    private uint _handle;

    public GpuDepthArray(GpuDevice device, string name, int width, int height, int layers)
    {
        if (width <= 0 || height <= 0 || layers <= 0) throw new GpuException($"A depth array of {width}x{height}x{layers} cannot be created", resource: name);
        _device = device;
        Name = name;
        Width = width;
        Height = height;
        Layers = layers;
        _handle = device.Gl.CreateTexture(TextureTarget.Texture2DArray);
        device.Gl.TextureStorage3D(_handle, 1, GpuFormats.Internal(GpuFormat.Depth32F), (uint)width, (uint)height, (uint)layers);
        device.Check("GpuDepthArray.Create", name);
    }

    public string Name { get; }

    public int Width { get; }

    public int Height { get; }

    public int Layers { get; }

    public uint Handle => _handle;

    /// <summary>The depth values of one layer, row 0 first, in the order GL stores them.</summary>
    public float[] Download(int layer)
    {
        if (layer < 0 || layer >= Layers) throw new GpuException($"Layer {layer} does not exist; the array has {Layers}", resource: Name);
        float[] depth = new float[Width * Height];
        _device.Gl.GetTextureSubImage(_handle, 0, 0, 0, layer, (uint)Width, (uint)Height, 1, GLEnum.DepthComponent, GLEnum.Float, (uint)(depth.Length * sizeof(float)), new Span<float>(depth));
        _device.Check("GpuDepthArray.Download", Name);
        return depth;
    }

    public void Dispose()
    {
        if (_handle == 0) return;
        _device.Gl.DeleteTexture(_handle);
        _handle = 0;
    }
}
