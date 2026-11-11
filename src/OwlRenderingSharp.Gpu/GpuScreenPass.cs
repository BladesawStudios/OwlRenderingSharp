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

    /// <summary>A vertex stage for a decompiled game fragment program: the fullscreen triangle with its texture coordinate in <c>out_attr0</c>, where the game's own vertex stage puts it.</summary>
    public const string GameVertex = """
        #version 450 core
        layout (location = 0) out vec4 out_attr0;
        void main()
        {
            vec2 p = vec2((gl_VertexID << 1) & 2, gl_VertexID & 2);
            out_attr0 = vec4(p, 0.0, 1.0);
            gl_Position = vec4(p * 2.0 - 1.0, 0.0, 1.0);
        }
        """;

    private readonly GpuDevice _device;
    private readonly GpuProgram _program;
    private readonly bool _ownsProgram;
    private readonly GpuVertexArray _vertices;
    private readonly GpuSampler _sampler;

    // The programs of passes built from source, linked once for each device and kept: a pass is made anew for every frame that uses it, and linking its program each time was the cost.
    private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<GpuDevice, Dictionary<string, GpuProgram>> Linked = new();

    public GpuScreenPass(GpuDevice device, string name, string fragmentSource)
    {
        _device = device;
        Name = name;
        Dictionary<string, GpuProgram> programs = Linked.GetOrCreateValue(device);
        string key = name + "\0" + fragmentSource;
        if (!programs.TryGetValue(key, out GpuProgram? program))
            programs[key] = program = new GpuProgram(device, name, new Dictionary<GpuShaderStage, string> { [GpuShaderStage.Vertex] = Vertex, [GpuShaderStage.Fragment] = fragmentSource });
        _program = program;
        _vertices = new GpuVertexArray(device, name);
        _sampler = new GpuSampler(device, name, SamplerState.LinearClamp);
    }

    /// <summary>A pass over a program the caller linked (its vertex stage <see cref="GameVertex"/>); the pass does not dispose it.</summary>
    public GpuScreenPass(GpuDevice device, string name, GpuProgram program)
    {
        _device = device;
        Name = name;
        _program = program;
        _ownsProgram = false;
        _vertices = new GpuVertexArray(device, name);
        _sampler = new GpuSampler(device, name, SamplerState.LinearClamp);
    }

    public string Name { get; }

    public void Run(GpuFramebuffer target, IReadOnlyList<GpuTexture> inputs, GpuBuffer? parameters = null) => Run(target, [.. inputs.Select(i => i.Handle)], parameters);

    /// <summary>The same with GL texture handles as inputs, for textures that are not 2D colour textures (such as a depth array read with <c>texelFetch</c>).</summary>
    public void Run(GpuFramebuffer target, IReadOnlyList<uint> inputs, GpuBuffer? parameters = null) => Run(target, inputs, parameters is null ? [] : [(0u, parameters)]);

    /// <summary>The same with several uniform buffers, each at its binding point; the inputs are bound at the texture units the program's samplers name (a zero handle leaves a unit empty). <paramref name="samplers"/> are optional GL sampler objects by unit, which replace the pass's clamped linear sampler (a comparison sampler for a shadow map).</summary>
    public void Run(GpuFramebuffer target, IReadOnlyList<uint> inputs, IReadOnlyList<(uint Binding, GpuBuffer Buffer)> blocks, IReadOnlyList<uint>? samplers = null)
    {
        GL gl = _device.Gl;
        _device.Apply(new DrawState(CullMode.None, false, false, DepthFunc.Always, BlendState.Off));
        gl.BindFramebuffer(FramebufferTarget.Framebuffer, target.Handle);
        gl.Viewport(0, 0, (uint)target.Width, (uint)target.Height);
        gl.UseProgram(_program.Handle);
        for (int i = 0; i < inputs.Count; i++)
        {
            if (inputs[i] == 0) continue;
            gl.BindTextureUnit((uint)i, inputs[i]);
            gl.BindSampler((uint)i, samplers is not null && i < samplers.Count && samplers[i] != 0 ? samplers[i] : _sampler.Handle);
        }

        foreach ((uint binding, GpuBuffer buffer) in blocks) gl.BindBufferBase(BufferTargetARB.UniformBuffer, binding, buffer.Handle);
        gl.BindVertexArray(_vertices.Handle);
        gl.DrawArrays(PrimitiveType.Triangles, 0, 3);
        _device.Check("GpuScreenPass.Run", Name);
    }

    public void Dispose()
    {
        _sampler.Dispose();
        _vertices.Dispose();
        if (_ownsProgram) _program.Dispose();
    }
}
