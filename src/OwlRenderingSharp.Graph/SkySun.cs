using System.Numerics;

namespace OwlRenderingSharp.Graph;

/// <summary>A bright lobe of light added to the stand-in reflection cube where the sun is, so polished surfaces show a highlight and not only a flat sky.</summary>
/// <remarks>
/// The colour is added in HDR on top of the sky, falling off as the cosine to the direction raised to <see cref="Sharpness"/>. The game's baked probe holds a real sun and clouds that this
/// library does not have, so the lobe is a stand-in whose strength is chosen by eye.
/// </remarks>
public sealed record SkySun(Vector3 Direction, Vector3 Color, float Sharpness = 96f)
{
    public Vector3 At(Vector3 direction) => Color * MathF.Pow(MathF.Max(Vector3.Dot(Vector3.Normalize(direction), Vector3.Normalize(Direction)), 0f), Sharpness);
}
