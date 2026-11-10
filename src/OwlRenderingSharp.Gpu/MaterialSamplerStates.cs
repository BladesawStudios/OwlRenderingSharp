using BfShaderSharp.Pack.Records;

namespace OwlRenderingSharp.Gpu;

/// <summary>Turns a material's sampler record into a <see cref="SamplerState"/>.</summary>
/// <remarks>
/// The record's filter word packs three two-bit filters: the mip filter in bits 0-1, magnification in bits 2-3 and minification in bits 4-5, each
/// 0 none, 1 points, 2 linear. Wrap values are the model file's (repeat, mirror, clamp, clamp to edge, mirror once, mirror once to edge); a
/// clamp that GL has no separate mode for is the edge clamp. A non-zero compare function makes it a depth-comparison sampler.
/// </remarks>
public static class MaterialSamplerStates
{
    public static SamplerState From(SamplerRecord record)
    {
        int mip = record.FilterFlags & 3, mag = record.FilterFlags >> 2 & 3, min = record.FilterFlags >> 4 & 3;
        return new SamplerState(
            Min(min == 2, mip),
            mag == 1 ? TextureFilter.Nearest : TextureFilter.Linear,
            Wrap(record.WrapU),
            Wrap(record.WrapV),
            Math.Max(1, (int)record.Anisotropy),
            record.MinLod,
            record.MaxLod,
            record.LodBias,
            record.Compare != 0);
    }

    private static TextureFilter Min(bool linear, int mip) => (linear, mip) switch
    {
        (false, 0) => TextureFilter.Nearest,
        (true, 0) => TextureFilter.Linear,
        (false, 1) => TextureFilter.NearestMipNearest,
        (true, 1) => TextureFilter.LinearMipNearest,
        (false, _) => TextureFilter.NearestMipLinear,
        (true, _) => TextureFilter.LinearMipLinear,
    };

    private static TextureWrap Wrap(byte wrap) => wrap switch
    {
        0 => TextureWrap.Repeat,
        1 => TextureWrap.Mirror,
        4 or 5 => TextureWrap.MirrorClamp,
        _ => TextureWrap.Clamp,
    };
}
