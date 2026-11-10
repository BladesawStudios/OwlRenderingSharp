using System.Numerics;
using OwlRenderingSharp.Content.Palettes;
using OwlRenderingSharp.Graph;

namespace OwlRenderingSharp.Games.Oot;

/// <summary>Builds the post chain settings of a frame from a palette's bloom and white balance fields.</summary>
/// <remarks>
/// The palette gives <c>BloomThreshold</c> (in stops, so the threshold is two to its power), <c>BloomIntensity</c>, <c>BloomComposePower</c> and <c>BloomClampedLuminance</c>, and a white balance as a
/// colour temperature (6500 is neutral) and a green-magenta tint. How the game applies each is not read from its post shaders (they are in a package this library cannot open); the stops reading of the threshold
/// and the power on the blurred bloom are assumptions. The balance multipliers come from a blackbody approximation of the temperature divided by the same at 6500, and the tint scales green by one minus the tint.
/// The exposure and the tone curve are not the palette's: the shaders already write exposed light, and the curve is <see cref="FilmicTonemap"/>.
/// </remarks>
public static class OotPost
{
    public const float DefaultExposure = 0.25f;

    public static PostSettings From(EnvPalette palette, float exposure = DefaultExposure) => new(
        exposure,
        MathF.Pow(2f, palette.GetFloat("BloomThreshold", 0f)),
        palette.GetFloat("BloomIntensity", 0f),
        palette.GetFloat("BloomComposePower", 1f),
        palette.GetFloat("BloomClampedLuminance", 10f),
        WhiteBalance(palette.GetFloat("WhiteBalanceTemperature", 6500f), palette.GetFloat("WhiteBalanceTint", 0f)));

    public static Vector3 WhiteBalance(float temperature, float tint)
    {
        Vector3 reference = Blackbody(6500f), actual = Blackbody(temperature);
        Vector3 balance = reference / actual;
        return new Vector3(balance.X, balance.Y * (1f - tint), balance.Z);
    }

    // Tanner Helland's fit of a blackbody colour, in linear 0..1.
    private static Vector3 Blackbody(float kelvin)
    {
        float t = Math.Clamp(kelvin, 1000f, 40000f) / 100f;
        float r = t <= 66f ? 255f : 329.698727446f * MathF.Pow(t - 60f, -0.1332047592f);
        float g = t <= 66f ? 99.4708025861f * MathF.Log(t) - 161.1195681661f : 288.1221695283f * MathF.Pow(t - 60f, -0.0755148492f);
        float b = t >= 66f ? 255f : t <= 19f ? 0f : 138.5177312231f * MathF.Log(t - 10f) - 305.0447927307f;
        static float Linear(float c) => MathF.Pow(Math.Clamp(c, 0f, 255f) / 255f, 2.2f);
        return new Vector3(Linear(r), Linear(g), Math.Max(Linear(b), 1e-3f));
    }
}
