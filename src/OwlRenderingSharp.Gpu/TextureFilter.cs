namespace OwlRenderingSharp.Gpu;

/// <summary>How a texture is filtered when it is magnified or minified.</summary>
/// <remarks>The mip filters apply to minification only; <see cref="Nearest"/> and <see cref="Linear"/> use no mips.</remarks>
public enum TextureFilter
{
    Nearest,
    Linear,
    NearestMipNearest,
    NearestMipLinear,
    LinearMipNearest,
    LinearMipLinear,
}
