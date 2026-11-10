using System.Numerics;

namespace OwlRenderingSharp.Graph;

/// <summary>Turns the linear, unbounded colour the shading programs write into the 8-bit sRGB of a picture.</summary>
/// <remarks>
/// The programs write light in physical-ish units (a sunlit surface is well above one), and the game compresses it after shading. This is the Narkowicz fit of the ACES curve
/// on the colour times an exposure, then the sRGB transfer. It is a presentation choice and not something read from the game; the exposure is the knob. It is pure arithmetic.
/// </remarks>
public static class FilmicTonemap
{
    public static float Curve(float x) => Math.Clamp(x * (2.51f * x + 0.03f) / (x * (2.43f * x + 0.59f) + 0.14f), 0f, 1f);

    public static float Srgb(float linear) => linear <= 0.0031308f ? linear * 12.92f : 1.055f * MathF.Pow(linear, 1f / 2.4f) - 0.055f;

    public static byte Encode(float linear, float exposure) => (byte)Math.Round(Srgb(Curve(Math.Max(0f, linear) * exposure)) * 255f);

    // Four floats per pixel in, four bytes out; alpha is kept as it is.
    public static byte[] ToBytes(ReadOnlySpan<float> rgba, float exposure)
    {
        byte[] bytes = new byte[rgba.Length];
        for (int i = 0; i < rgba.Length; i += 4)
        {
            bytes[i] = Encode(rgba[i], exposure);
            bytes[i + 1] = Encode(rgba[i + 1], exposure);
            bytes[i + 2] = Encode(rgba[i + 2], exposure);
            bytes[i + 3] = (byte)Math.Round(Math.Clamp(rgba[i + 3], 0f, 1f) * 255f);
        }

        return bytes;
    }
}
