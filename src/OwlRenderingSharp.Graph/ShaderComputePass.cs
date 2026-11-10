using BfShaderSharp.Bnsh;
using BfShaderSharp.Errors;
using BfShaderSharp.Prepared;
using OwlRenderingSharp.Content.Programs;
using OwlRenderingSharp.Gpu;

namespace OwlRenderingSharp.Graph;

/// <summary>One compute program of a prepared shader archive (the game's compute post passes) dispatched over an image.</summary>
/// <remarks>
/// The program's samplers are given by their engine names and its uniform blocks by their engine block names as bytes, padded to the size the program declares, as for <see cref="ShaderScreenPass"/>.
/// The program writes one image at image unit zero, which the caller gives. The compute stage's blocks and samplers are numbered in the archive in the vertex slots, which is where the
/// bindings read them. The dispatch covers the image with groups of the program's local size, which the caller gives since it is written in the program. It is used from the GL thread only and disposes what it made.
/// </remarks>
public sealed class ShaderComputePass : IDisposable
{
    private readonly RenderServices _services;
    private readonly GpuProgram _program;
    private readonly GpuComputePass _pass;
    private readonly ProgramBindings _bindings;
    private readonly IReadOnlyDictionary<string, int> _blockSizes;
    private readonly string _name;

    public ShaderComputePass(RenderServices services, string archive, string model, int program, int localSizeX, int localSizeY)
    {
        _services = services;
        LocalSizeX = localSizeX;
        LocalSizeY = localSizeY;
        _name = $"{archive}/{model} program {program}";
        PreparedModel prepared = services.Content.Shaders(archive).Model(model) ?? throw new BfShaderFormatException($"{archive} has no shading model {model}");
        if (program < 0 || program >= prepared.ProgramCount) throw new BfShaderFormatException($"{_name} does not exist");
        string compute = GlslCompatibility.Rewrite(prepared.Glsl(program, ShaderStage.Compute) ?? throw new BfShaderFormatException($"{_name} has no compute stage"));
                _bindings = ProgramBindings.For(prepared, program, compute, null);
        _blockSizes = prepared.Blocks().GroupBy(b => b.Name).ToDictionary(g => g.Key, g => g.Max(b => b.ByteSize));
        _program = new GpuProgram(services.Device, _name, new Dictionary<GpuShaderStage, string> { [GpuShaderStage.Compute] = compute });
        foreach (BlockBinding block in _bindings.Blocks) _program.TryBindUniformBlock(block.GlslName, (uint)services.Points.Get(block.GsysName));
        _pass = new GpuComputePass(services.Device, _name, _program);
    }

    public int LocalSizeX { get; }

    public int LocalSizeY { get; }

    public void Run(GpuTexture output, IReadOnlyDictionary<string, uint> samplers, IReadOnlyDictionary<string, byte[]> blocks)
    {
        uint[] units = new uint[_bindings.Samplers.Select(s => s.Unit + 1).DefaultIfEmpty(0).Max()];
        foreach (SamplerBinding sampler in _bindings.Samplers)
            units[sampler.Unit] = samplers.TryGetValue(sampler.ShaderName, out uint handle) ? handle : _services.Neutral.Handle(GpuSamplerKind.Texture2D, 0xFF000000);
        List<GpuBuffer> buffers = [];
        List<(uint, GpuBuffer)> bound = [];
        try
        {
            foreach (BlockBinding block in _bindings.Blocks)
            {
                if (!blocks.TryGetValue(block.GsysName, out byte[]? bytes)) throw new GpuException($"The program reads the uniform block {block.GsysName} and none was given", resource: _name);
                GpuBuffer buffer = new(_services.Device, $"{_name} {block.GsysName}", Math.Max(bytes.Length, _blockSizes.GetValueOrDefault(block.GsysName)));
                buffers.Add(buffer);
                buffer.Write<byte>(bytes);
                bound.Add(((uint)_services.Points.Get(block.GsysName), buffer));
            }

            _pass.Run((output.Width + LocalSizeX - 1) / LocalSizeX, (output.Height + LocalSizeY - 1) / LocalSizeY, units, [(0u, output)], bound);
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
