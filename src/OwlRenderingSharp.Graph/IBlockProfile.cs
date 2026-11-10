using OwlRenderingSharp.Blocks;

namespace OwlRenderingSharp.Graph;

/// <summary>Knows a game's uniform blocks: which are frame or shape level, where their fields sit and how to fill them.</summary>
/// <remarks>
/// The drawer asks the profile about each block a program declares. A block without a layout is left zero, which is how a block is stubbed until its
/// behaviour is worked out. The shape is null while a frame-level block is filled.
/// </remarks>
public interface IBlockProfile
{
    BlockScope Scope(string block);

    // The shape is null for frame-level blocks. A shape block may be sized by its shape, such as a skeleton palette by its bone count.
    BlockLayout? Layout(string block, ShapeInfo? shape);

    void Fill(string block, BlockWriter writer, FrameInfo frame, ShapeInfo? shape);
}
