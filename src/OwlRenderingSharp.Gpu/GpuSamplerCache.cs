namespace OwlRenderingSharp.Gpu;

/// <summary>Shares one <see cref="GpuSampler"/> among every material whose sampler state is equal.</summary>
/// <remarks>It is used from the GL thread only and disposes the samplers it made.</remarks>
public sealed class GpuSamplerCache(GpuDevice device) : IDisposable
{
    private readonly Dictionary<SamplerState, GpuSampler> _samplers = [];

    public int Count => _samplers.Count;

    public GpuSampler Get(SamplerState state)
    {
        if (!_samplers.TryGetValue(state, out GpuSampler? sampler)) _samplers[state] = sampler = new GpuSampler(device, $"sampler {_samplers.Count}", state);
        return sampler;
    }

    public void Dispose()
    {
        foreach (GpuSampler sampler in _samplers.Values) sampler.Dispose();
        _samplers.Clear();
    }
}
