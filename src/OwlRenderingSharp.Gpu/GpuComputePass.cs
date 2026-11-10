using Silk.NET.OpenGL;

namespace OwlRenderingSharp.Gpu;

/// <summary>A compute program dispatched over an image, for the game's compute post passes.</summary>
/// <remarks>
/// The program is linked by the caller from a compute stage and is not disposed here. Inputs are bound as textures at the units the program's samplers name (a zero handle leaves a unit empty),
/// sampled with a clamped linear sampler; outputs are bound as write-only images at their image units in the format of the texture; uniform buffers go at their binding points. A memory barrier
/// after the dispatch makes the written images visible to later passes and reads.
/// </remarks>
public sealed class GpuComputePass : IDisposable
{
    private readonly GpuDevice _device;
    private readonly GpuProgram _program;
    private readonly GpuSampler _sampler;

    public GpuComputePass(GpuDevice device, string name, GpuProgram program)
    {
        _device = device;
        Name = name;
        _program = program;
        _sampler = new GpuSampler(device, name, SamplerState.LinearClamp);
    }

    public string Name { get; }

    public void Run(int groupsX, int groupsY, IReadOnlyList<uint> textures, IReadOnlyList<(uint Unit, GpuTexture Image)> images, IReadOnlyList<(uint Binding, GpuBuffer Buffer)> blocks)
    {
        if (groupsX < 1 || groupsY < 1) throw new GpuException($"A dispatch needs at least one group in each direction, not {groupsX}x{groupsY}", resource: Name);
        GL gl = _device.Gl;
        gl.UseProgram(_program.Handle);
        for (int i = 0; i < textures.Count; i++)
        {
            if (textures[i] == 0) continue;
            gl.BindTextureUnit((uint)i, textures[i]);
            gl.BindSampler((uint)i, _sampler.Handle);
        }

        foreach ((uint unit, GpuTexture image) in images)
            gl.BindImageTexture(unit, image.Handle, 0, false, 0, BufferAccessARB.WriteOnly, (InternalFormat)GpuFormats.Internal(image.Format));
        foreach ((uint binding, GpuBuffer buffer) in blocks) gl.BindBufferBase(BufferTargetARB.UniformBuffer, binding, buffer.Handle);
        gl.DispatchCompute((uint)groupsX, (uint)groupsY, 1);
        gl.MemoryBarrier(MemoryBarrierMask.ShaderImageAccessBarrierBit | MemoryBarrierMask.TextureFetchBarrierBit | MemoryBarrierMask.FramebufferBarrierBit);
        _device.Check("GpuComputePass.Run", Name);
    }

    public void Dispose() => _sampler.Dispose();
}
