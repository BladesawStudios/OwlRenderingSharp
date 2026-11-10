using System.Numerics;
using OwlRenderingSharp.Blocks;
using OwlRenderingSharp.Graph;

namespace OwlRenderingSharp.Snapshot;

/// <summary>A profile that wraps another and replaces one field of one block with a value after the wrapped profile has filled it, to see what the field does to the picture.</summary>
/// <remarks>It writes the field as a vector, so it only fits Vector4 fields.</remarks>
public sealed class AblationProfile(IBlockProfile inner, string block, string field, Vector4 value) : IBlockProfile
{
    public BlockScope Scope(string name) => inner.Scope(name);

    public BlockLayout? Layout(string name, ShapeInfo? shape) => inner.Layout(name, shape);

    public void Fill(string name, BlockWriter writer, FrameInfo frame, ShapeInfo? shape)
    {
        inner.Fill(name, writer, frame, shape);
        if (name == block) writer.Set(field, value);
    }
}
