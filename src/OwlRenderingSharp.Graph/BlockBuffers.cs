using OwlRenderingSharp.Gpu;

namespace OwlRenderingSharp.Graph;

/// <summary>Uniform buffers kept by key, each created at the size first asked for.</summary>
/// <remarks>A frame block is kept under its name; a shape block under the shape and the name. A key asked for again with a different size throws <see cref="GpuException"/>.</remarks>
public sealed class BlockBuffers(GpuDevice device) : IDisposable
{
    private readonly Dictionary<string, GpuBuffer> _buffers = new(StringComparer.Ordinal);

    public GpuBuffer Get(string key, int size)
    {
        if (!_buffers.TryGetValue(key, out GpuBuffer? buffer)) _buffers[key] = buffer = new GpuBuffer(device, key, size);
        else if (buffer.ByteSize != size) throw new GpuException($"The block {key} was {buffer.ByteSize} bytes and is now asked for at {size}", resource: key);
        return buffer;
    }

    public void Dispose()
    {
        foreach (GpuBuffer buffer in _buffers.Values) buffer.Dispose();
        _buffers.Clear();
    }
}
