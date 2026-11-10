using Silk.NET.OpenGL;

namespace OwlRenderingSharp.Gpu;

/// <summary>A GL sampler object configured from a <see cref="SamplerState"/>.</summary>
/// <remarks>It is immutable after creation; a different state is a different sampler. A depth comparison sampler compares less-or-equal.</remarks>
public sealed class GpuSampler : IDisposable
{
    private readonly GpuDevice _device;
    private uint _handle;

    public GpuSampler(GpuDevice device, string name, SamplerState state)
    {
        _device = device;
        Name = name;
        State = state;
        GL gl = device.Gl;
        _handle = gl.CreateSampler();
        gl.SamplerParameter(_handle, SamplerParameterI.MinFilter, (int)Min(state.Min));
        gl.SamplerParameter(_handle, SamplerParameterI.MagFilter, (int)(state.Mag == TextureFilter.Nearest ? GLEnum.Nearest : GLEnum.Linear));
        gl.SamplerParameter(_handle, SamplerParameterI.WrapS, (int)Wrap(state.WrapU));
        gl.SamplerParameter(_handle, SamplerParameterI.WrapT, (int)Wrap(state.WrapV));
        gl.SamplerParameter(_handle, SamplerParameterF.MinLod, state.MinLod);
        gl.SamplerParameter(_handle, SamplerParameterF.MaxLod, state.MaxLod);
        gl.SamplerParameter(_handle, SamplerParameterF.LodBias, state.LodBias);
        if (state.Anisotropy > 1f) gl.SamplerParameter(_handle, SamplerParameterF.MaxAnisotropy, state.Anisotropy);
        if (state.Compare)
        {
            gl.SamplerParameter(_handle, SamplerParameterI.CompareMode, (int)GLEnum.CompareRefToTexture);
            gl.SamplerParameter(_handle, SamplerParameterI.CompareFunc, (int)GLEnum.Lequal);
        }

        device.Check("GpuSampler.Create", name);
    }

    public string Name { get; }

    public SamplerState State { get; }

    public uint Handle => _handle;

    public void Dispose()
    {
        if (_handle == 0) return;
        _device.Gl.DeleteSampler(_handle);
        _handle = 0;
    }

    private static GLEnum Min(TextureFilter filter) => filter switch
    {
        TextureFilter.Nearest => GLEnum.Nearest,
        TextureFilter.Linear => GLEnum.Linear,
        TextureFilter.NearestMipNearest => GLEnum.NearestMipmapNearest,
        TextureFilter.NearestMipLinear => GLEnum.NearestMipmapLinear,
        TextureFilter.LinearMipNearest => GLEnum.LinearMipmapNearest,
        TextureFilter.LinearMipLinear => GLEnum.LinearMipmapLinear,
        _ => throw new ArgumentOutOfRangeException(nameof(filter)),
    };

    private static GLEnum Wrap(TextureWrap wrap) => wrap switch
    {
        TextureWrap.Repeat => GLEnum.Repeat,
        TextureWrap.Mirror => GLEnum.MirroredRepeat,
        TextureWrap.Clamp => GLEnum.ClampToEdge,
        TextureWrap.Border => GLEnum.ClampToBorder,
        TextureWrap.MirrorClamp => GLEnum.MirrorClampToEdge,
        _ => throw new ArgumentOutOfRangeException(nameof(wrap)),
    };
}
