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
    private const int StorageSlots = 4, StorageBytes = 65536;
    private readonly RenderServices _services;
    private readonly IBlockProfile _profile;
    private readonly GpuCommands _commands;
    private readonly BlockBuffers _blocks;
    private readonly GpuBuffer _support;
    private readonly GpuBuffer _storage;

    public ModelDrawer(RenderServices services, IBlockProfile profile)
    {
        _services = services;
        _profile = profile;
        _commands = new GpuCommands(services.Device);
        _blocks = new BlockBuffers(services.Device);
        _support = new GpuBuffer(services.Device, "support buffer", SupportBufferData.Size);
        _storage = new GpuBuffer(services.Device, "zero storage", StorageBytes);
    }

    public void Draw(ModelResources model, FrameInfo frame, Matrix4x4 world)
    {
        _support.Write<byte>(SupportBufferData.Create(frame.Width, frame.Height));
        _commands.BindUniform(BlockBindingPoints.SupportBuffer, _support);
        for (uint slot = 0; slot < StorageSlots; slot++) _commands.BindStorage(slot, _storage);
        HashSet<string> frameBlocks = [];
        foreach (ShapeResources shape in model.Shapes)
        {
            MaterialResources material = model.Materials[shape.MaterialIndex];
            ShapeInfo info = new(shape.Name, shape.Record, shape.BoneIndex, model.Skeleton, world);
            Prepare(model.Name, material, info, frame, frameBlocks);
            _services.Device.Apply(material.State);
            foreach (TextureUnit unit in material.Units) _commands.BindTexture((uint)unit.Unit, unit.Texture, unit.Sampler);
            _commands.Draw(shape.Geometry.Meshes[0], "ModelDrawer.Draw", $"{model.Name}/{shape.Name}");
        }
    }

    public void Dispose()
    {
        _blocks.Dispose();
        _support.Dispose();
        _storage.Dispose();
    }

    private void Prepare(string model, MaterialResources material, ShapeInfo shape, FrameInfo frame, HashSet<string> frameBlocks)
    {
        _commands.UseProgram(material.Program.Program);
        foreach (BlockBinding block in material.Program.Bindings.Blocks.DistinctBy(b => b.GsysName))
        {
            uint point = (uint)_services.Points.Get(block.GsysName);
            switch (_profile.Scope(block.GsysName))
            {
                case BlockScope.Material when material.Block is not null:
                    _commands.BindUniform(point, material.Block);
                    break;
                case BlockScope.Frame when frameBlocks.Add(block.GsysName):
                    _commands.BindUniform(point, Fill(block.GsysName, block.GsysName, material.Program, frame, null));
                    break;
                case BlockScope.Frame:
                    break;
                case BlockScope.Shape:
                    _commands.BindUniform(point, Fill($"{model}/{shape.Name}/{block.GsysName}", block.GsysName, material.Program, frame, shape));
                    break;
                default:
                    _commands.BindUniform(point, _blocks.Get(block.GsysName, material.Program.BlockSizes[block.GsysName]));
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
