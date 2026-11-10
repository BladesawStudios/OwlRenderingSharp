namespace OwlRenderingSharp.Gpu;

/// <summary>The colour channels a draw writes.</summary>
[Flags]
public enum ColorMask
{
    None = 0,
    Red = 1,
    Green = 2,
    Blue = 4,
    Alpha = 8,
    All = Red | Green | Blue | Alpha,
}
