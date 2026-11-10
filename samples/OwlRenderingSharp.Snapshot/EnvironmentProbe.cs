using System.Numerics;
using OwlRenderingSharp.Blocks;
using OwlRenderingSharp.Graph;

namespace OwlRenderingSharp.Snapshot;

/// <summary>A profile that wraps another and overwrites a range of environment block slots with one value, to find which slots a program reads by looking at the render.</summary>
/// <remarks>The environment block is laid out as 114 vec4 slots named S0 to S113 in place of the wrapped profile's layout. Slots outside the range stay zero.</remarks>
public sealed class EnvironmentProbe(IBlockProfile inner, int first, int last, Vector4 value) : IBlockProfile
{
    private const string Block = "gsys_environment";
    private static readonly BlockLayout Slots = new(Block, 114 * 16, [.. Enumerable.Range(0, 114).Select(i => new BlockField($"S{i}", i * 16, BlockFieldType.Vector4, "probe"))]);

    public BlockScope Scope(string block) => inner.Scope(block);

    public BlockLayout? Layout(string block, ShapeInfo? shape) => block == Block ? Slots : inner.Layout(block, shape);

    public void Fill(string block, BlockWriter writer, FrameInfo frame, ShapeInfo? shape)
    {
        if (block != Block)
        {
            inner.Fill(block, writer, frame, shape);
            return;
        }

        for (int i = first; i <= last && i < 114; i++) writer.Set($"S{i}", value);
    }
}
