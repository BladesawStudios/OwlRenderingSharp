namespace OwlRenderingSharp.Gpu;

/// <summary>Which faces a draw discards.</summary>
/// <remarks>Front faces are counter-clockwise.</remarks>
public enum CullMode
{
    None,
    Back,
    Front,
    Both,
}
