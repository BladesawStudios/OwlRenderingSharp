using System.Numerics;
using OwlRenderingSharp.Blocks;
using OwlRenderingSharp.Graph;

namespace OwlRenderingSharp.Snapshot;

/// <summary>A profile that wraps another and, after it fills a block, writes raw vec4 slots into the bytes, so a slot the layout does not name can be tried by looking at the render.</summary>
public sealed class SlotPatchProfile(IBlockProfile inner, string block, IReadOnlyDictionary<int, Vector4> slots) : IBlockProfile
{
    public BlockScope Scope(string name) => inner.Scope(name);

    public BlockLayout? Layout(string name, ShapeInfo? shape) => inner.Layout(name, shape);

    public void Fill(string name, BlockWriter writer, FrameInfo frame, ShapeInfo? shape)
    {
        inner.Fill(name, writer, frame, shape);
        if (name != block) return;
        foreach ((int slot, Vector4 value) in slots)
        {
            if ((slot + 1) * 16 > writer.Bytes.Length) continue;
            for (int i = 0; i < 4; i++) BitConverter.TryWriteBytes(writer.Bytes.AsSpan(slot * 16 + i * 4), value[i]);
        }
    }
}
