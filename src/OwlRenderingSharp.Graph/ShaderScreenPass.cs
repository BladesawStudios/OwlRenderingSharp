using BfShaderSharp.Bnsh;
using BfShaderSharp.Errors;
using BfShaderSharp.Prepared;
using OwlRenderingSharp.Content.Programs;
using OwlRenderingSharp.Gpu;

namespace OwlRenderingSharp.Graph;

/// <summary>One program of a prepared shader archive (the game's own post-processing shaders) drawn over a whole target.</summary>
/// <remarks>
/// The fragment stage is the archive's decompiled GLSL; the vertex stage is the library's fullscreen triangle, which gives the texture coordinate the game's own vertex stage gives. Samplers are
/// given by the engine name the program reads them by (<c>cHDRImage</c>) and uniform blocks by their engine block name (<c>HdrComposeParam</c>) as bytes, which are padded to the size the program
/// declares. A sampler or block the program reads and the caller left out is refused by name. It is used from the GL thread only and disposes what it made.
/// </remarks>
public sealed class ShaderScreenPass : IDisposable
{
    private readonly RenderServices _services;
    private readonly GpuProgram _program;
    private readonly GpuScreenPass _pass;
    private readonly ProgramBindings _bindings;
    private readonly IReadOnlyDictionary<string, int> _blockSizes;
    private readonly string _name;

    public ShaderScreenPass(RenderServices services, string archive, string model, int program)
    {
        _services = services;
        _name = $"{archive}/{model} program {program}";
        PreparedModel prepared = services.Content.Shaders(archive).Model(model) ?? throw new BfShaderFormatException($"{archive} has no shading model {model}");
        if (program < 0 || program >= prepared.ProgramCount) throw new BfShaderFormatException($"{_name} does not exist");
        string vertex = prepared.Glsl(program, ShaderStage.Vertex) ?? "", fragment = prepared.Glsl(program, ShaderStage.Fragment) ?? throw new BfShaderFormatException($"{_name} has no fragment stage");
        fragment = GlslCompatibility.Rewrite(fragment);
        _bindings = ProgramBindings.For(prepared, program, vertex.Length == 0 ? null : GlslCompatibility.Rewrite(vertex), fragment);
        _blockSizes = prepared.Blocks().GroupBy(b => b.Name).ToDictionary(g => g.Key, g => g.Max(b => b.ByteSize));
        _program = new GpuProgram(services.Device, _name, new Dictionary<GpuShaderStage, string> { [GpuShaderStage.Vertex] = GpuScreenPass.GameVertex, [GpuShaderStage.Fragment] = fragment });
        foreach (BlockBinding block in _bindings.Blocks.Where(b => !b.Vertex)) _program.TryBindUniformBlock(block.GlslName, (uint)services.Points.Get(block.GsysName));
        _pass = new GpuScreenPass(services.Device, _name, _program);
    }

    public void Run(GpuFramebuffer target, IReadOnlyDictionary<string, uint> samplers, IReadOnlyDictionary<string, byte[]> blocks, IReadOnlyDictionary<string, uint>? samplerObjects = null)
    {
        uint[] units = new uint[_bindings.Samplers.Where(s => !s.Vertex).Select(s => s.Unit + 1).DefaultIfEmpty(0).Max()];
        foreach (SamplerBinding sampler in _bindings.Samplers.Where(s => !s.Vertex))
            units[sampler.Unit] = samplers.TryGetValue(sampler.ShaderName, out uint handle) ? handle : _services.Neutral.Handle(GpuSamplerKind.Texture2D, 0xFF000000);
        List<GpuBuffer> buffers = [];
        List<(uint, GpuBuffer)> bound = [];
        try
        {
            foreach (BlockBinding block in _bindings.Blocks.Where(b => !b.Vertex))
            {
                if (!blocks.TryGetValue(block.GsysName, out byte[]? bytes)) throw new GpuException($"The program reads the uniform block {block.GsysName} and none was given", resource: _name);
                GpuBuffer buffer = new(_services.Device, $"{_name} {block.GsysName}", Math.Max(bytes.Length, _blockSizes.GetValueOrDefault(block.GsysName)));
                buffers.Add(buffer);
                buffer.Write<byte>(bytes);
                bound.Add(((uint)_services.Points.Get(block.GsysName), buffer));
            }

            uint[] objects = new uint[units.Length];
            if (samplerObjects is not null)
                foreach (SamplerBinding sampler in _bindings.Samplers.Where(s => !s.Vertex && samplerObjects.ContainsKey(s.ShaderName))) objects[sampler.Unit] = samplerObjects[sampler.ShaderName];
            _pass.Run(target, units, bound, objects);
        }
        finally
        {
            foreach (GpuBuffer buffer in buffers) buffer.Dispose();
        }
    }

    public void Dispose()
    {
        _pass.Dispose();
        _program.Dispose();
    }
}
