using System.Numerics;

namespace OwlRenderingSharp.Graph;

/// <summary>What a snapshot draws besides the model: where it stands, the shadow map that catches its shadow, and the textures the frame provides.</summary>
/// <remarks>
/// With a <see cref="ShadowMap"/> the model first casts into its cascades from the sun (<see cref="ToSun"/>, out to <see cref="ShadowDistance"/>) and the colour pass reads them.
/// <see cref="Textures"/> are frame textures by sampler name, such as a <see cref="SkyProbe"/>'s cube; the shadow map's own texture is added to them.
/// </remarks>
public sealed record SnapshotScene(
    Matrix4x4? Placement = null,
    ShadowMap? Shadows = null,
    Vector3 ToSun = default,
    float ShadowDistance = 50f,
    IReadOnlyDictionary<string, FrameTexture>? Textures = null)
{
    public FrameInfo Frame(Camera camera, int width, int height)
    {
        FrameInfo frame = Shadows?.Frame(camera, width, height) ?? new FrameInfo(camera, width, height);
        if (Textures is null) return frame;
        Dictionary<string, FrameTexture> merged = new(frame.Textures ?? new Dictionary<string, FrameTexture>());
        foreach ((string name, FrameTexture texture) in Textures) merged[name] = texture;
        return frame with { Textures = merged };
    }
}
