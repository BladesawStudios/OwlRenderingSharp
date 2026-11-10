using Silk.NET.OpenGL;

namespace OwlRenderingSharp.Gpu;

/// <summary>A fragment program drawn over a whole target with a single triangle, for post-processing.</summary>
/// <remarks>
/// The vertex stage is built in; the caller supplies the fragment source, which reads its inputs from <c>sampler2D</c>s at binding 0, 1, ... in the order given to <c>Run</c>, and
/// may read a uniform block declared at binding 0. The pass draws without culling, depth or blending and sets the viewport to the target. Inputs are sampled with a clamped linear sampler.
/// </remarks>
public sealed class GpuScreenPass : IDisposable
{
    private const string Vertex = "#version 450 core\nout vec2 uv;\nvoid main()\n{\n    vec2 p = vec2((gl_VertexID << 1) & 2, gl_VertexID & 2);\n    uv = p;\n    gl_Position = vec4(p * 2.0 - 1.0, 0.0, 1.0);\n}\n";

    private readonly GpuDevice _device;
    private readonly GpuProgram _program;
    private readonly GpuVertexArray _vertices;
    private readonly GpuSampler _sampler;

    public GpuScreenPass(GpuDevice device, string name, string fragmentSource)
    {
        _device = device;
        Name = name;
        _program = new GpuProgram(device, name, new Dictionary<GpuShaderStage, string> { [GpuShaderStage.Vertex] = Vertex, [GpuShaderStage.Fragment] = fragmentSource });
        _vertices = new GpuVertexArray(device, name);
        _sampler = new GpuSampler(device, name, SamplerState.LinearClamp);
    }

    public string Name { get; }

    public void Run(GpuFramebuffer target, IReadOnlyList<GpuTexture> inputs, GpuBuffer? parameters = null) => Run(target, [.. inputs.Select(i => i.Handle)], parameters);

    /// <summary>The same with GL texture handles as inputs, for textures that are not 2D colour textures (such as a depth array read with <c>texelFetch</c>).</summary>
    public void Run(GpuFramebuffer target, IReadOnlyList<uint> inputs, GpuBuffer? parameters = null)
    {
        GL gl = _device.Gl;
        _device.Apply(new DrawState(CullMode.None, false, false, DepthFunc.Always, BlendState.Off));
        gl.BindFramebuffer(FramebufferTarget.Framebuffer, target.Handle);
        gl.Viewport(0, 0, (uint)target.Width, (uint)target.Height);
        gl.UseProgram(_program.Handle);
        for (int i = 0; i < inputs.Count; i++)
        {
            gl.BindTextureUnit((uint)i, inputs[i]);
            gl.BindSampler((uint)i, _sampler.Handle);
        }

        if (parameters is not null) gl.BindBufferBase(BufferTargetARB.UniformBuffer, 0, parameters.Handle);
        gl.BindVertexArray(_vertices.Handle);
        gl.DrawArrays(PrimitiveType.Triangles, 0, 3);
        _device.Check("GpuScreenPass.Run", Name);
    }

    public void Dispose()
    {
        _sampler.Dispose();
        _vertices.Dispose();
        _program.Dispose();
    }
}
