namespace OwlRenderingSharp.Content.Geometry;

/// <summary>How the stored value of a vertex component becomes the value a shader reads.</summary>
/// <remarks>
/// Normalized kinds become floats in 0 to 1 or -1 to 1. The ToFloat kinds convert an integer to the float of the same value. UInt and SInt keep the
/// integer, which the game's shaders read as the bit pattern of the float register (<c>floatBitsToInt</c>), so they are written as raw bits.
/// </remarks>
public enum VertexValueKind
{
    UNorm,
    SNorm,
    UInt,
    SInt,
    Float,
    UIntToFloat,
    SIntToFloat,
}
