using OwlRenderingSharp.Content;
using OwlRenderingSharp.Gpu;

namespace OwlRenderingSharp.Graph;

/// <summary>The device, the content and the caches that everything drawn in one renderer shares.</summary>
/// <remarks>
/// Programs, textures, geometry, samplers and neutral textures are created once here however many models use them. The host creates it over a current GL context,
/// uses it from that thread only and disposes it before the context goes; it does not dispose the device or the content it is given.
/// </remarks>
public sealed class RenderServices : IDisposable
{
    public RenderServices(GpuDevice device, ContentSet content)
    {
        Device = device;
        Content = content;
        Points = new BlockBindingPoints();
        Programs = new ProgramCache(device, content, Points);
        Resources = new GpuResourceCache(device, content);
        Samplers = new GpuSamplerCache(device);
        Neutral = new GpuNeutralTextures(device);
    }

    public GpuDevice Device { get; }

    public ContentSet Content { get; }

    public BlockBindingPoints Points { get; }

    public ProgramCache Programs { get; }

    public GpuResourceCache Resources { get; }

    public GpuSamplerCache Samplers { get; }

    public GpuNeutralTextures Neutral { get; }

    public void Dispose()
    {
        Programs.Dispose();
        Resources.Dispose();
        Samplers.Dispose();
        Neutral.Dispose();
    }
}
