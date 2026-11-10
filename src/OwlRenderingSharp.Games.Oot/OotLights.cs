using System.Numerics;
using OwlRenderingSharp.Blocks;
using OwlRenderingSharp.Content.Palettes;
using OwlRenderingSharp.Graph;

namespace OwlRenderingSharp.Games.Oot;

/// <summary>Writes the light slots of the environment block from a palette.</summary>
/// <remarks>
/// The palette gives its lights in physical units that its exposure value brings back to a working range, so the sun is its diffuse colour times its intensity divided
/// by two to the power of the exposure value (a palette of intensity 10000 at exposure 10 gives a sun near 9.8). The hemisphere light is its top and bottom colours times the hemisphere
/// intensity times <see cref="HemiScale"/>. The palette marks the hemisphere intensity as exposure-linked, but how is unknown; at its full value (8 against a sun of 9.8) the ambient is nearly as strong as the sun and flattens every surface,
/// so the scale is a visual choice that keeps the sun the main light, to be replaced once the real conversion is found.
/// The main light is written as the direction it travels in world space, the negative of the direction to the sun. Azimuth is measured from +z toward +x and elevation up
/// from the horizon; both are assumptions until a render shows the shadows fall the right way. The ambient colour is fixed, as in WildRenderingSharp.
/// </remarks>
public static class OotLights
{
    private const float DefaultExposure = 10f;

    /// <summary>What the palette's hemisphere intensity is multiplied by; chosen by eye (0.4 keeps the sun's shading visible, 1 flattens it).</summary>
    public const float HemiScale = 0.4f;

    /// <summary>The distance in metres from the camera to the subject that the character light rig is laid out for, and the strengths of its three lights as shares of the sun.</summary>
    public const float CharacterDistance = 3f, KeyScale = 0.5f, FillScale = 0.2f, RimScale = 0.3f;

    private static readonly Vector4 Ambient = new(0.10f, 0.11f, 0.13f, 1f);

    public static void Fill(BlockWriter writer, OotEnvironment environment)
    {
        EnvPalette palette = environment.Palette;
        float exposure = 1f / MathF.Pow(2f, palette.GetFloat("ExposureValue", DefaultExposure));
        Vector4 diffuse = palette.GetColor("BgDifColor", Vector4.One);
        Vector3 sun = new Vector3(diffuse.X, diffuse.Y, diffuse.Z) * palette.GetFloat("BgDifIntensity", 1f / exposure) * exposure;
        (Vector3 sky, Vector3 ground) = SkyColors(environment);

        writer.Set("SunColor", new Vector4(sun, 1f)).Set("LightColor0", new Vector4(sun, 1f));
        writer.Set("LightDirectionWorld", new Vector4(-Vector3.Normalize(ToSun(environment)), 0f));
        writer.Set("HemiSky", new Vector4(sky, 1f)).Set("HemiGround", new Vector4(ground, 1f));
        writer.Set("AmbientColor", Ambient);
        FillNeutral(writer);
        FillCharacterLights(writer, sun);
    }

    // The three point lights the character programs read, as a rig fixed to the camera: a key high on the left of the lens, a weaker fill low on the right and a rim behind the subject.
    private static void FillCharacterLights(BlockWriter writer, Vector3 sun)
    {
        float d = CharacterDistance;
        writer.Set("PointLightPosition0", new Vector4(-0.6f * d, 0.6f * d, -0.2f * d, 0f)).Set("PointLightColor0", new Vector4(sun * KeyScale, 1f));
        writer.Set("PointLightPosition1", new Vector4(0.7f * d, -0.1f * d, -0.2f * d, 0f)).Set("PointLightColor1", new Vector4(sun * FillScale, 1f));
        writer.Set("PointLightPosition2", new Vector4(0.4f * d, 0.5f * d, -1.6f * d, 0f)).Set("PointLightColor2", new Vector4(sun * RimScale, 1f));
        writer.Set("PointLightRange", new Vector4(1f, 0f, 0f, 0f));
        writer.Set("PointLightRange1", new Vector4(0f, 0f, 0f, 0f));
    }

    // The hemisphere light's colours, which also stand in for the sky and the ground a reflection would show.
    public static (Vector3 Sky, Vector3 Ground) SkyColors(OotEnvironment environment)
    {
        EnvPalette palette = environment.Palette;
        float hemi = palette.GetFloat("HemiLightIntensity", 1f);
        Vector4 top = palette.GetColor("HemiLightColorTop", Vector4.One), bottom = palette.GetColor("HemiLightColorBottom", new Vector4(0.5f, 0.5f, 0.5f, 1f));
        return (new Vector3(top.X, top.Y, top.Z) * hemi * HemiScale, new Vector3(bottom.X, bottom.Y, bottom.Z) * hemi * HemiScale);
    }

    // The slots with no palette field behind them yet, set to values that leave their terms inert.
    private static void FillNeutral(BlockWriter writer)
    {
        writer.Set("AmbientScale", new Vector4(1f, 0f, 0f, 0f));
        writer.Set("ShadowScale", new Vector4(1f, 1f, 1f, 1f));
        writer.Set("TerminatorRamp", new Vector4(0f, 1f, 0f, 0f));
        writer.Set("LightVolume", new Vector4(0.5f, 0.5f, 0.5f, 0f));
        writer.Set("LightVolumeBlend", Vector4.One);
        writer.Set("ShadowTint", new Vector4(1f, 1f, 0f, 0f));
        writer.Set("SkinRamp", new Vector4(1f, 0f, 0f, 1f));
        writer.Set("SkinScales", Vector4.One).Set("SurfaceScales", new Vector4(1f, 1f, 1f, 0f)).Set("SpecularScales", Vector4.One);
        writer.Set("DepthLimit", new Vector4(float.MaxValue, 0f, 0f, 0f));
        writer.Set("ReflectionLod", new Vector4(SkyProbe.MipLevels - 1, 0f, 0f, 0f));
    }

    // The sky a reflection shows: the hemisphere's sky colour cooled toward the palette's sky-sun colour at the same brightness, and a lobe of the key light's colour where the sun is.
    public static (Vector3 Sky, SkySun Sun) ProbeSky(OotEnvironment environment, float lobe, float cool)
    {
        EnvPalette palette = environment.Palette;
        (Vector3 sky, _) = SkyColors(environment);
        Vector4 tint = palette.GetColor("SkySunColor", Vector4.One);
        Vector3 cold = new(tint.X, tint.Y, tint.Z);
        float skyLuma = Vector3.Dot(sky, new Vector3(0.2126f, 0.7152f, 0.0722f)), coldLuma = Math.Max(Vector3.Dot(cold, new Vector3(0.2126f, 0.7152f, 0.0722f)), 1e-3f);
        Vector3 cooled = Vector3.Lerp(sky, cold * (skyLuma / coldLuma), cool);
        return (cooled, new SkySun(ToSun(environment), Vector3.One * skyLuma * 6f * lobe));
    }

    // The unit vector from the scene toward the sun, in world space.
    public static Vector3 ToSun(OotEnvironment environment)
    {
        float azimuth = float.DegreesToRadians(environment.SunAzimuth), elevation = float.DegreesToRadians(environment.SunElevation);
        return new Vector3(MathF.Cos(elevation) * MathF.Sin(azimuth), MathF.Sin(elevation), MathF.Cos(elevation) * MathF.Cos(azimuth));
    }
}
