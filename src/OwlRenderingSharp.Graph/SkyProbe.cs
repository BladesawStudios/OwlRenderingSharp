using System.Numerics;
using OwlRenderingSharp.Gpu;

namespace OwlRenderingSharp.Graph;

/// <summary>A stand-in reflection cube built from a sky colour above and a ground colour below, offered to the frame as <c>gsys_cube_map</c>.</summary>
/// <remarks>
/// The game reflects a baked probe of the place; with none, a constant cube makes every glossy surface one flat colour. The sides take the average of sky and ground, so a vertical
/// surface reflects the horizon. Colours are linear, clamped to zero to one, and stored as 8-bit. It is used from the GL thread only and disposes its texture.
/// </remarks>
public sealed class SkyProbe : IDisposable
{
    public const string SamplerName = "gsys_cube_map";
    private readonly GpuCubeTexture _cube;
    private readonly FrameTexture _texture;

    public SkyProbe(RenderServices services, Vector3 sky, Vector3 ground)
    {
        Vector3 side = (sky + ground) * 0.5f;
        _cube = new GpuCubeTexture(services.Device, "sky probe", [Pack(side), Pack(side), Pack(sky), Pack(ground), Pack(side), Pack(side)]);
        _texture = new FrameTexture(_cube.Handle, services.Samplers.Get(SamplerState.LinearClamp).Handle);
    }

    public IReadOnlyDictionary<string, FrameTexture> Textures => new Dictionary<string, FrameTexture> { [SamplerName] = _texture };

    public void Dispose() => _cube.Dispose();

    private static uint Pack(Vector3 color)
    {
        Vector3 c = Vector3.Clamp(color, Vector3.Zero, Vector3.One) * 255f + new Vector3(0.5f);
        return (uint)c.X | (uint)c.Y << 8 | (uint)c.Z << 16 | 0xFF000000u;
    }
}
