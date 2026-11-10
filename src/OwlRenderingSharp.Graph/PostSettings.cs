using System.Numerics;

namespace OwlRenderingSharp.Graph;

/// <summary>What the screen post chain does to a linear image: the exposure, the bloom and the white balance.</summary>
/// <remarks>
/// <see cref="Exposure"/> multiplies the linear light before the tone curve. Bloom takes what is brighter than <see cref="BloomThreshold"/> (in linear light, after the exposure), clamped to
/// <see cref="BloomClamp"/>, blurs it at a quarter of the size and adds <see cref="BloomIntensity"/> times its <see cref="BloomPower"/>-th power back; an intensity of zero turns bloom off.
/// <see cref="WhiteBalance"/> multiplies the colour channels. The default is the plain exposure and curve with no bloom and a neutral balance.
/// </remarks>
public sealed record PostSettings(
    float Exposure = 0.25f,
    float BloomThreshold = 1f,
    float BloomIntensity = 0f,
    float BloomPower = 1f,
    float BloomClamp = 10f,
    Vector3? WhiteBalance = null)
{
    public Vector3 Balance => WhiteBalance ?? Vector3.One;
}
