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

    BlockLayout? Layout(string block);

    void Fill(string block, BlockWriter writer, FrameInfo frame, ShapeInfo? shape);
}
