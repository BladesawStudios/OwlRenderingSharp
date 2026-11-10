using System.Numerics;
using OwlRenderingSharp.Gpu;

namespace OwlRenderingSharp.Graph;

/// <summary>The cascaded shadow map of a frame: a depth array with one layer per slice of the view frustum, rendered from the sun.</summary>
/// <remarks>
/// <see cref="Render"/> fits the cascades to the viewer (see <see cref="CascadeFit"/>), then for each layer targets it, clears it and calls the caller's draw with a frame whose
/// camera is that layer's light camera; the caller draws its casters with <see cref="PassPlan.Shadow"/>, which runs each material's depth shadow program. The caller targets its own
/// colour target again afterwards. <see cref="Frame"/> gives the frame the shaded pass draws with: it carries the cascades, which the profile writes into the context block, and the
/// depth array under the engine's <c>gsys_depth_shadow_cascade</c> name with a comparison sampler. It is used from the GL thread only and disposes its GPU objects.
/// </remarks>
public sealed class ShadowMap : IDisposable
{
    public const string SamplerName = "gsys_depth_shadow_cascade";
    private readonly RenderServices _services;
    private readonly GpuDepthArray _depth;
    private readonly GpuDepthLayerTarget[] _targets;
    private readonly GpuCommands _commands;
    private readonly FrameTexture _texture;

    public ShadowMap(RenderServices services, int resolution = 2048, int cascades = 3)
    {
        _services = services;
        _commands = new GpuCommands(services.Device);
        Resolution = resolution;
        _depth = new GpuDepthArray(services.Device, "shadow cascades", resolution, resolution, cascades);
        _targets = [.. Enumerable.Range(0, cascades).Select(i => new GpuDepthLayerTarget(services.Device, $"shadow cascade {i}", _depth, i))];
        uint sampler = services.Samplers.Get(new SamplerState(TextureFilter.Linear, TextureFilter.Linear, TextureWrap.Clamp, TextureWrap.Clamp, Compare: true)).Handle;
        _texture = new FrameTexture(_depth.Handle, sampler);
    }

    public int Resolution { get; }

    /// <summary>The GL handle of the depth array, for passes that read it directly.</summary>
    public uint DepthHandle => _depth.Handle;

    public IReadOnlyList<ShadowCascade> Cascades { get; private set; } = [];

    public void Render(Camera viewer, float aspect, Vector3 toSun, float shadowDistance, Action<FrameInfo> drawCasters)
    {
        Cascades = CascadeFit.Fit(viewer, aspect, toSun, _targets.Length, shadowDistance, Resolution);
        for (int i = 0; i < _targets.Length; i++)
        {
            _commands.UseZeroToOneDepth();
            _commands.TargetDepth(_targets[i]);
            _commands.Clear(0f, 0f, 0f, 0f);
            drawCasters(new FrameInfo(Cascades[i].Light, Resolution, Resolution));
        }
    }

    // The frame to draw the shaded pass with: the viewer's camera and size, the cascades just rendered, and the shadow map under its sampler name.
    public FrameInfo Frame(Camera viewer, int width, int height) =>
        new(viewer, width, height, Cascades, new Dictionary<string, FrameTexture> { [SamplerName] = _texture });

    // The depth values of one cascade's layer, for tools that want to look at what was cast.
    public float[] Download(int cascade) => _depth.Download(cascade);

    public void Dispose()
    {
        foreach (GpuDepthLayerTarget target in _targets) target.Dispose();
        _depth.Dispose();
    }
}
