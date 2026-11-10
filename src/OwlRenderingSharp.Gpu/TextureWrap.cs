namespace OwlRenderingSharp.Gpu;

/// <summary>What a coordinate outside 0 to 1 reads.</summary>
/// <remarks>These are the wrap modes the games' sampler records use; a border reads transparent black.</remarks>
public enum TextureWrap
{
    Repeat,
    Mirror,
    Clamp,
    Border,
    MirrorClamp,
}
