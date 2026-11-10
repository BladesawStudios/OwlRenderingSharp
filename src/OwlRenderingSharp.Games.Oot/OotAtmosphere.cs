using System.Numerics;
using OwlRenderingSharp.Content.Palettes;
using OwlRenderingSharp.Graph;

namespace OwlRenderingSharp.Games.Oot;

/// <summary>The distance fog and depth of field of a frame, read from a place's palette and the game's depth-of-field defaults.</summary>
/// <remarks>
/// <c>FogColor</c>, <c>FogStart</c> and <c>FogEnd</c> come from the palette. The depth-of-field focal distance and range (50 and 50 metres in the main field's parameters of the game's own palette)
/// are not in the place palettes, so they are this class's defaults, and the blur radius is this library's choice. Neither changes a lone character within a few metres of the camera.
/// </remarks>
public static class OotAtmosphere
{
    public const float FocalDistance = 50f, FocalRange = 50f, MaxBlurRadius = 6f;

    /// <summary>The palette's fog; <paramref name="density"/> multiplies how thick it is by dividing the distance from where it starts to where it is full (1 is the palette's own, 0 or less is no fog).</summary>
    public static SnapshotScene.FogSettings? Fog(EnvPalette palette, float density = 1f)
    {
        if (density <= 0f) return null;
        float start = palette.GetFloat("FogStart", 0f), end = palette.GetFloat("FogEnd", 600f);
        return new(palette.GetColor("FogColor", Vector4.Zero), start, start + (end - start) / density);
    }

    public static SnapshotScene.FocusSettings Focus(float supersample = 1f) => new(FocalDistance, FocalRange, MaxBlurRadius * supersample);
}
