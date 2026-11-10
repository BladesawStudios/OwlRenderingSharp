namespace OwlRenderingSharp.Gpu;

/// <summary>A vertex array object; one with no attributes is what a draw that makes its vertices in the shader binds.</summary>
/// <remarks>Core profile requires one to be bound for any draw.</remarks>
public sealed class GpuVertexArray : IDisposable
{
    private readonly GpuDevice _device;
    private uint _handle;

    public GpuVertexArray(GpuDevice device, string name)
    {
        _device = device;
        Name = name;
        _handle = device.Gl.CreateVertexArray();
        device.Check("GpuVertexArray.Create", name);
    }

    public string Name { get; }

    public uint Handle => _handle;

    public void Dispose()
    {
        if (_handle == 0) return;
        _device.Gl.DeleteVertexArray(_handle);
        _handle = 0;
    }
}
