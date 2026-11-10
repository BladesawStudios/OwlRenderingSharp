using Silk.NET.OpenGL;

namespace OwlRenderingSharp.Gpu;

/// <summary>The state changes and draw calls of a pass, in terms of the library's own GPU objects so that code above this project never sees GL.</summary>
/// <remarks>
/// Binding points are the numbers programs were moved to, not slots of any one stage. Depth is zero to one, as the game's projections produce, so a pass begins by calling
/// <see cref="UseZeroToOneDepth"/>. Every method that can fail ends in the device's error check, naming the pass the caller gives.
/// </remarks>
public sealed class GpuCommands(GpuDevice device)
{
    public void UseZeroToOneDepth() => device.Gl.ClipControl(ClipControlOrigin.LowerLeft, ClipControlDepth.ZeroToOne);

    public void Target(GpuFramebuffer? framebuffer, int width, int height)
    {
        device.Gl.BindFramebuffer(FramebufferTarget.Framebuffer, framebuffer?.Handle ?? 0);
        device.Gl.Viewport(0, 0, (uint)width, (uint)height);
    }

    public void Clear(float red, float green, float blue, float alpha)
    {
        device.Apply(DrawState.Opaque);
        device.Gl.ClearColor(red, green, blue, alpha);
        device.Gl.ClearDepth(1.0);
        device.Gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
    }

    public void UseProgram(GpuProgram program) => device.Gl.UseProgram(program.Handle);

    public void BindUniform(uint point, GpuBuffer buffer) => device.Gl.BindBufferBase(BufferTargetARB.UniformBuffer, point, buffer.Handle);

    public void BindStorage(uint point, GpuBuffer buffer) => device.Gl.BindBufferBase(BufferTargetARB.ShaderStorageBuffer, point, buffer.Handle);

    public void BindTexture(uint unit, uint texture, uint sampler)
    {
        device.Gl.BindTextureUnit(unit, texture);
        device.Gl.BindSampler(unit, sampler);
    }

    public unsafe void Draw(GpuMesh mesh, string pass, string resource)
    {
        device.Gl.BindVertexArray(mesh.VertexArray);
        device.Gl.DrawElementsBaseVertex(PrimitiveType.Triangles, (uint)mesh.IndexCount, mesh.IndexType, null, mesh.FirstVertex);
        device.Check(pass, resource);
    }
}
