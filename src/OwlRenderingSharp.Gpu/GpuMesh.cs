using Silk.NET.OpenGL;

namespace OwlRenderingSharp.Gpu;

/// <summary>One level of detail of a <see cref="GpuGeometry"/>: an index buffer and a vertex array over the geometry's vertex buffer.</summary>
/// <remarks>The vertex array binds every attribute the geometry holds, as four 32-bit words at its shader input location. Indices are relative to the first vertex.</remarks>
public sealed class GpuMesh : IDisposable
{
    private readonly GpuDevice _device;
    private uint _vertexArray;

    internal GpuMesh(GpuDevice device, string name, GpuBuffer vertices, int stride, IReadOnlyList<int> locations, GpuBuffer indices, int indexCount, bool wide, int firstVertex)
    {
        _device = device;
        Indices = indices;
        IndexCount = indexCount;
        IndexType = wide ? DrawElementsType.UnsignedInt : DrawElementsType.UnsignedShort;
        FirstVertex = firstVertex;
        GL gl = device.Gl;
        _vertexArray = gl.CreateVertexArray();
        gl.VertexArrayVertexBuffer(_vertexArray, 0, vertices.Handle, 0, (uint)stride);
        for (int i = 0; i < locations.Count; i++)
        {
            uint location = (uint)locations[i];
            gl.EnableVertexArrayAttrib(_vertexArray, location);
            gl.VertexArrayAttribFormat(_vertexArray, location, 4, VertexAttribType.Float, false, (uint)(i * 16));
            gl.VertexArrayAttribBinding(_vertexArray, location, 0);
        }

        gl.VertexArrayElementBuffer(_vertexArray, indices.Handle);
        device.Check("GpuMesh.Create", name);
    }

    public GpuBuffer Indices { get; }

    public int IndexCount { get; }

    public DrawElementsType IndexType { get; }

    public int FirstVertex { get; }

    public uint VertexArray => _vertexArray;

    public void Dispose()
    {
        if (_vertexArray == 0) return;
        _device.Gl.DeleteVertexArray(_vertexArray);
        _vertexArray = 0;
        Indices.Dispose();
    }
}
