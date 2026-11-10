namespace OwlRenderingSharp.Gpu;

/// <summary>Where a sampled channel gets its value: a constant or one of the stored channels.</summary>
/// <remarks>The numbers are the ones BNTX stores in a texture's channel swizzle, so a stored byte casts straight to this.</remarks>
public enum GpuChannel : byte
{
    Zero = 0,
    One = 1,
    Red = 2,
    Green = 3,
    Blue = 4,
    Alpha = 5,
}
