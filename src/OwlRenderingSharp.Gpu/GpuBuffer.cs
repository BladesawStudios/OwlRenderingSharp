using Silk.NET.OpenGL;

namespace OwlRenderingSharp.Gpu;

/// <summary>A GL buffer object of a fixed size, created with direct state access.</summary>
/// <remarks>
/// Contents are written and read as spans of unmanaged values; a write or read that does not fit the buffer throws a
/// <see cref="GpuException"/> naming the buffer. The buffer is deleted when disposed, on the device's thread.
/// </remarks>
public sealed class GpuBuffer : IDisposable
{
    private readonly GpuDevice _device;
    private uint _handle;

    public GpuBuffer(GpuDevice device, string name, int byteSize, BufferStorageMask flags = BufferStorageMask.DynamicStorageBit)
    {
        _device = device;
        Name = name;
        ByteSize = byteSize;
        _handle = device.Gl.CreateBuffer();
        device.Gl.NamedBufferStorage(_handle, (nuint)byteSize, ReadOnlySpan<byte>.Empty, flags);
        device.Check("GpuBuffer.Create", name);
    }

    public string Name { get; }

    public int ByteSize { get; }

    public uint Handle => _handle;

    public void Write<T>(ReadOnlySpan<T> data, int byteOffset = 0) where T : unmanaged
    {
        int bytes = data.Length * System.Runtime.CompilerServices.Unsafe.SizeOf<T>();
        Fit(byteOffset, bytes);
        _device.Gl.NamedBufferSubData(_handle, byteOffset, (nuint)bytes, data);
        _device.Check("GpuBuffer.Write", Name);
    }

    public void Read<T>(Span<T> destination, int byteOffset = 0) where T : unmanaged
    {
        int bytes = destination.Length * System.Runtime.CompilerServices.Unsafe.SizeOf<T>();
        Fit(byteOffset, bytes);
        _device.Gl.GetNamedBufferSubData(_handle, byteOffset, (nuint)bytes, destination);
        _device.Check("GpuBuffer.Read", Name);
    }

    public void Dispose()
    {
        if (_handle == 0) return;
        _device.Gl.DeleteBuffer(_handle);
        _handle = 0;
    }

    private void Fit(int byteOffset, int bytes)
    {
        if (byteOffset < 0 || byteOffset + bytes > ByteSize)
            throw new GpuException($"{bytes} bytes at offset {byteOffset} do not fit a buffer of {ByteSize} bytes", resource: Name);
    }
}
