namespace OwlRenderingSharp.Gpu;

/// <summary>The dimensionality of a sampler a shader declares.</summary>
/// <remarks>It decides which neutral texture can stand in where a program samples something the renderer does not provide.</remarks>
public enum GpuSamplerKind
{
    Texture2D,
    Texture2DShadow,
    Texture2DArray,
    Texture3D,
    TextureCube,
    Other,
}
