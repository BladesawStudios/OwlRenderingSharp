using System.Numerics;
using OwlRenderingSharp.Blocks;
using OwlRenderingSharp.Content.Programs;
using OwlRenderingSharp.Gpu;

namespace OwlRenderingSharp.Graph;

/// <summary>Draws a model's shapes at their first level of detail with the program, textures and blocks of their materials.</summary>
/// <remarks>
/// Frame-level blocks are filled and bound once per draw, shape-level blocks once per shape, and the material block is the one prepared in the pack. Which blocks
/// exist and how they are filled is the <see cref="IBlockProfile"/>'s business; a block it does not lay out stays zero. The support buffer and the zeroed storage
/// buffers the programs expect are bound for every draw. The caller has already chosen the target and cleared it. It is used from the GL thread only and disposes its buffers.
/// </remarks>
public sealed class ModelDrawer : IDisposable
{
    private const int StorageSlots = 4, StorageBytes = 16777216;
    private const float ShadowSlopeBias = 2f, ShadowUnitBias = 4f;
    private readonly RenderServices _services;
    private readonly IBlockProfile _profile;
    private readonly GpuCommands _commands;
    private readonly BlockBuffers _blocks;
    private readonly GpuBuffer _support;
    private readonly GpuBuffer _storage;
    private readonly GpuBuffer _probes;
    private bool _hasProbes;

    public ModelDrawer(RenderServices services, IBlockProfile profile)
    {
        _services = services;
        _profile = profile;
        _commands = new GpuCommands(services.Device);
        _blocks = new BlockBuffers(services.Device);
        _support = new GpuBuffer(services.Device, "support buffer", SupportBufferData.Size);
        _storage = new GpuBuffer(services.Device, "zero storage", StorageBytes);
        _probes = new GpuBuffer(services.Device, "light probes", StorageBytes);
        _probes.Write<byte>(new byte[StorageBytes]);
        _storage.Write<byte>(new byte[StorageBytes]);
    }

    public void SetLightProbes(ReadOnlySpan<float> entries)
    {
        _probes.Write<float>(entries, ShProbe.EntryBytes);
        _hasProbes = true;
    }

    public void Draw(ModelResources model, FrameInfo frame, Matrix4x4 world) => Draw(model, frame, world, PassPlan.Forward);

    public void Draw(ModelResources model, FrameInfo frame, Matrix4x4 world, PassPlan plan)
    {
        _support.Write<byte>(SupportBufferData.Create(frame.Width, frame.Height));
        _commands.BindUniform(BlockBindingPoints.SupportBuffer, _support);
        for (uint slot = 0; slot < StorageSlots; slot++) _commands.BindStorage(slot, slot == 0 && _hasProbes ? _probes : _storage);
        HashSet<string> frameBlocks = [];
        foreach (RenderPass pass in plan.Passes)
            foreach (ShapeResources shape in Ordered(model, pass))
                DrawShape(model, shape, pass, frame, world, frameBlocks);
    }

    public void Dispose()
    {
        _blocks.Dispose();
        _support.Dispose();
        _storage.Dispose();
        _probes.Dispose();
    }

    // Opaque shapes come before blended ones, each group by the material's priority and then in model order. A depth-only pass leaves the blended shapes out.
    private static IEnumerable<ShapeResources> Ordered(ModelResources model, RenderPass pass) => model.Shapes
        .Select((shape, index) => (shape, index, material: model.Materials[shape.MaterialIndex]))
        .Where(s => !(pass.DepthOnly() && s.material.Blends))
        .OrderBy(s => s.material.Blends).ThenBy(s => s.material.Priority).ThenBy(s => s.index)
        .Select(s => s.shape);

    private void DrawShape(ModelResources model, ShapeResources shape, RenderPass pass, FrameInfo frame, Matrix4x4 world, HashSet<string> frameBlocks)
    {
        MaterialResources material = model.Materials[shape.MaterialIndex];
        MaterialPass? chosen = material.Pass(pass, shape.Record.SkinCount);
        if (chosen is null) return;
        ShapeInfo info = new(shape.Name, shape.Record, shape.BoneIndex, model.Skeleton, world);
        _support.Write<byte>(SupportBufferData.Create(frame.Width, frame.Height, material.AlphaTest));
        Prepare(model.Name, material, chosen.Program, info, frame, frameBlocks);
        _services.Device.Apply(PassState(material.State, pass));
        foreach (TextureUnit unit in chosen.Units)
        {
            FrameTexture bound = frame.Textures is not null && frame.Textures.TryGetValue(unit.ShaderName, out FrameTexture replacement) ? replacement : new FrameTexture(unit.Texture, unit.Sampler);
            _commands.BindTexture((uint)unit.Unit, bound.Texture, bound.Sampler);
        }

        _commands.Draw(shape.Geometry.Meshes[0], $"ModelDrawer.Draw {pass}", $"{model.Name}/{shape.Name}");
    }

    // A depth-only pass writes no colour and does not blend; the shadow pass also biases its depth so a surface does not shadow itself.
    private static DrawState PassState(DrawState state, RenderPass pass)
    {
        if (!pass.DepthOnly()) return state;
        DrawState depthOnly = state with { Channels = ColorMask.None, Blend = BlendState.Off };
        return pass == RenderPass.DepthShadow ? depthOnly with { Raster = RasterState.Normal with { DepthBiasSlope = ShadowSlopeBias, DepthBiasUnits = ShadowUnitBias } } : depthOnly;
    }

    private void Prepare(string model, MaterialResources material, ProgramEntry program, ShapeInfo shape, FrameInfo frame, HashSet<string> frameBlocks)
    {
        _commands.UseProgram(program.Program);
        foreach (BlockBinding block in program.Bindings.Blocks.DistinctBy(b => b.GsysName))
        {
            uint point = (uint)_services.Points.Get(block.GsysName);
            switch (_profile.Scope(block.GsysName))
            {
                case BlockScope.Material when material.Block is not null:
                    _commands.BindUniform(point, material.Block);
                    break;
                case BlockScope.Frame when frameBlocks.Add(block.GsysName):
                    _commands.BindUniform(point, Fill(block.GsysName, block.GsysName, program, frame, null));
                    break;
                case BlockScope.Frame:
                    break;
                case BlockScope.Shape:
                    _commands.BindUniform(point, Fill($"{model}/{shape.Name}/{block.GsysName}", block.GsysName, program, frame, shape));
                    break;
                default:
                    _commands.BindUniform(point, _blocks.Get(block.GsysName, program.BlockSizes[block.GsysName]));
                    break;
            }
        }
    }

    private GpuBuffer Fill(string key, string block, ProgramEntry program, FrameInfo frame, ShapeInfo? shape)
    {
        BlockLayout? layout = _profile.Layout(block, shape);
        GpuBuffer buffer = _blocks.Get(key, Math.Max(program.BlockSizes[block], layout?.Size ?? 0));
        if (layout is null) return buffer;
        BlockWriter writer = new(layout);
        _profile.Fill(block, writer, frame, shape);
        buffer.Write<byte>(writer.Bytes.AsSpan(0, Math.Min(buffer.ByteSize, writer.Bytes.Length)));
        return buffer;
    }
}
