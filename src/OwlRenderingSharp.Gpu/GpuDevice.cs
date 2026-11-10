using Silk.NET.OpenGL;

namespace OwlRenderingSharp.Gpu;

/// <summary>The GL context a renderer draws with, and the error check every GPU operation ends in.</summary>
/// <remarks>
/// It is created by the host over a context that is current on the calling thread and is used only from that thread. Nothing in
/// the library holds a device statically; every resource is given the device it belongs to. <see cref="Check"/> drains the GL error queue
/// and throws one <see cref="GpuException"/> that lists all of it.
/// </remarks>
public sealed class GpuDevice(GL gl)
{
    private const int MaxDrain = 32;

    public GL Gl { get; } = gl;

    public void Apply(DrawState state)
    {
        SetCap(EnableCap.DepthTest, state.DepthTest);
        Gl.DepthMask(state.DepthWrite);
        Gl.DepthFunc(state.DepthFunc switch
        {
            DepthFunc.Never => GLEnum.Never,
            DepthFunc.Less => GLEnum.Less,
            DepthFunc.Equal => GLEnum.Equal,
            DepthFunc.LessOrEqual => GLEnum.Lequal,
            DepthFunc.Greater => GLEnum.Greater,
            DepthFunc.NotEqual => GLEnum.Notequal,
            DepthFunc.GreaterOrEqual => GLEnum.Gequal,
            _ => GLEnum.Always,
        });
        SetCap(EnableCap.CullFace, state.Cull != CullMode.None);
        if (state.Cull != CullMode.None) Gl.CullFace(state.Cull switch { CullMode.Front => TriangleFace.Front, CullMode.Both => TriangleFace.FrontAndBack, _ => TriangleFace.Back });
    }

    public void Check(string? pass = null, string? resource = null)
    {
        List<GLEnum> errors = [];
        for (int i = 0; i < MaxDrain; i++)
        {
            GLEnum error = Gl.GetError();
            if (error == GLEnum.NoError) break;
            errors.Add(error);
        }

        if (errors.Count > 0) throw new GpuException($"GL error: {string.Join(", ", errors)}", pass, resource);
    }

    private void SetCap(EnableCap cap, bool on)
    {
        if (on) Gl.Enable(cap);
        else Gl.Disable(cap);
    }
}
