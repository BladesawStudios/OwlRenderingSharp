namespace OwlRenderingSharp.Graph;

/// <summary>What the frame-level blocks are filled from: the camera, the size of the target being drawn to, and the shadow cascades and frame textures when the frame has them.</summary>
/// <remarks>
/// <paramref name="Cascades"/> are in order from the nearest slice. <paramref name="Textures"/> replace the neutral texture of a frame-level sampler of the same engine name
/// (<c>gsys_depth_shadow_cascade</c> for the shadow map); a frame without them draws as if nothing were shadowed.
/// </remarks>
public sealed record FrameInfo(Camera Camera, int Width, int Height, IReadOnlyList<ShadowCascade>? Cascades = null, IReadOnlyDictionary<string, FrameTexture>? Textures = null)
{
    public float Aspect => (float)Width / Math.Max(1, Height);
}
