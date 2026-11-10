namespace OwlRenderingSharp.Gpu;

/// <summary>The state a sampler object holds; value-equal states describe the same sampler.</summary>
/// <remarks>Anisotropy of 1 is off. Compare makes the sampler a depth-comparison sampler for shadow lookups.</remarks>
public readonly record struct SamplerState(
    TextureFilter Min,
    TextureFilter Mag,
    TextureWrap WrapU,
    TextureWrap WrapV,
    float Anisotropy = 1f,
    float MinLod = -1000f,
    float MaxLod = 1000f,
    float LodBias = 0f,
    bool Compare = false)
{
    public static SamplerState LinearClamp { get; } = new(TextureFilter.Linear, TextureFilter.Linear, TextureWrap.Clamp, TextureWrap.Clamp);
}
