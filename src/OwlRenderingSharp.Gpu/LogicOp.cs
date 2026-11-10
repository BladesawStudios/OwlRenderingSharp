namespace OwlRenderingSharp.Gpu;

/// <summary>A bitwise operation between the colour a draw outputs and the colour already in the target, used in place of blending.</summary>
/// <remarks><see cref="None"/> leaves the operation off. The other sixteen are the operations GX2 and NVN define.</remarks>
public enum LogicOp
{
    None,
    Clear,
    And,
    AndReverse,
    Copy,
    AndInverted,
    Noop,
    Xor,
    Or,
    Nor,
    Equivalent,
    Invert,
    OrReverse,
    CopyInverted,
    OrInverted,
    Nand,
    Set,
}
