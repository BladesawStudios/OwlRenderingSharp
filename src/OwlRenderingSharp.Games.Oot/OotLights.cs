using System.Numerics;
using OwlRenderingSharp.Blocks;
using OwlRenderingSharp.Content.Palettes;

namespace OwlRenderingSharp.Games.Oot;

/// <summary>Writes the light slots of the environment block from a palette.</summary>
/// <remarks>
/// The palette gives its lights in physical units that its exposure value brings back to a working range, so the sun is its diffuse colour times its intensity divided
/// by two to the power of the exposure value (a palette of intensity 10000 at exposure 10 gives a sun near 9.8). The hemisphere light is its top and bottom colours times the hemisphere
/// intensity, unscaled. TODO: whether the hemisphere intensity is also in exposure units (the palette marks it exposure-linked) is not checked against a render.
/// The main light is written as the direction it travels in world space, the negative of the direction to the sun. Azimuth is measured from +z toward +x and elevation up
/// from the horizon; both are assumptions until a render shows the shadows fall the right way. The ambient colour is fixed, as in WildRenderingSharp.
/// </remarks>
public static class OotLights
{
    private const float DefaultExposure = 10f;
    private static readonly Vector4 Ambient = new(0.10f, 0.11f, 0.13f, 1f);

    public static void Fill(BlockWriter writer, OotEnvironment environment)
    {
        EnvPalette palette = environment.Palette;
        float exposure = 1f / MathF.Pow(2f, palette.GetFloat("ExposureValue", DefaultExposure));
        Vector4 diffuse = palette.GetColor("BgDifColor", Vector4.One);
        Vector3 sun = new Vector3(diffuse.X, diffuse.Y, diffuse.Z) * palette.GetFloat("BgDifIntensity", 1f / exposure) * exposure;
        float hemi = palette.GetFloat("HemiLightIntensity", 1f);
        Vector4 top = palette.GetColor("HemiLightColorTop", Vector4.One), bottom = palette.GetColor("HemiLightColorBottom", new Vector4(0.5f, 0.5f, 0.5f, 1f));

        writer.Set("SunColor", new Vector4(sun, 1f)).Set("LightColor0", new Vector4(sun, 1f));
        writer.Set("LightDirectionWorld", new Vector4(-Vector3.Normalize(ToSun(environment)), 0f));
        writer.Set("HemiSky", new Vector4(top.X * hemi, top.Y * hemi, top.Z * hemi, 1f));
        writer.Set("HemiGround", new Vector4(bottom.X * hemi, bottom.Y * hemi, bottom.Z * hemi, 1f));
        writer.Set("AmbientColor", Ambient);
        FillNeutral(writer);
    }

    // The slots with no palette field behind them yet, set to values that leave their terms inert.
    private static void FillNeutral(BlockWriter writer)
    {
        writer.Set("AmbientScale", new Vector4(1f, 0f, 0f, 0f));
        writer.Set("ShadowScale", new Vector4(1f, 1f, 1f, 1f));
        writer.Set("TerminatorRamp", new Vector4(0f, 1f, 0f, 0f));
        writer.Set("LightVolume", new Vector4(0.5f, 0.5f, 0.5f, 0f));
        writer.Set("LightVolumeBlend", Vector4.One);
        writer.Set("PointLightPosition0", new Vector4(0f, 1e6f, 0f, 0f));
        writer.Set("PointLightPosition1", new Vector4(0f, 1e6f, 0f, 0f));
        writer.Set("PointLightRange", Vector4.One);
        writer.Set("PointLightRange1", Vector4.One);
        writer.Set("PointLightPosition2", new Vector4(0f, 1e6f, 0f, 0f));
        writer.Set("ShadowTint", new Vector4(1f, 1f, 0f, 0f));
        writer.Set("SkinRamp", new Vector4(1f, 0f, 0f, 1f));
        writer.Set("SkinScales", Vector4.One).Set("SurfaceScales", new Vector4(1f, 1f, 1f, 0f)).Set("SpecularScales", Vector4.One);
        writer.Set("DepthLimit", new Vector4(float.MaxValue, 0f, 0f, 0f));
    }

    // The unit vector from the scene toward the sun, in world space.
    public static Vector3 ToSun(OotEnvironment environment)
    {
        float azimuth = float.DegreesToRadians(environment.SunAzimuth), elevation = float.DegreesToRadians(environment.SunElevation);
        return new Vector3(MathF.Cos(elevation) * MathF.Sin(azimuth), MathF.Sin(elevation), MathF.Cos(elevation) * MathF.Cos(azimuth));
    }
}
