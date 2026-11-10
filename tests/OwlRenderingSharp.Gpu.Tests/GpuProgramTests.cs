using Silk.NET.OpenGL;

namespace OwlRenderingSharp.Gpu.Tests;

[Collection(GlCollection.Name)]
public class GpuProgramTests(GlFixture gl)
{
    private const string Vertex = "#version 450 core\nvoid main()\n{\n    vec2 p = vec2((gl_VertexID << 1) & 2, gl_VertexID & 2);\n    gl_Position = vec4(p * 2.0 - 1.0, 0.0, 1.0);\n}\n";

    private const string Fragment = "#version 450 core\nlayout (std140, binding = 0) uniform Tint { vec4 tint; };\nout vec4 color;\nvoid main()\n{\n    color = tint;\n}\n";

    private static Dictionary<GpuShaderStage, string> Sources(string fragment) =>
        new() { [GpuShaderStage.Vertex] = Vertex, [GpuShaderStage.Fragment] = fragment };

    [SkippableFact]
    public void AFullScreenTriangleFillsATargetWithAUniformBlockColor()
    {
        byte[] pixels = gl.Require().Run(device =>
        {
            using GpuProgram program = new(device, "fill", Sources(Fragment));
            program.BindUniformBlock("Tint", 3);
            using GpuBuffer tint = new(device, "tint", 16);
            tint.Write<float>([1f, 0f, 0.5f, 1f]);
            using GpuTexture target = new(device, "target", GpuFormat.Rgba8, 4, 4);
            using GpuFramebuffer framebuffer = new(device, "fb", [target]);
            using GpuVertexArray vertices = new(device, "none");
            GL g = device.Gl;
            g.BindFramebuffer(FramebufferTarget.Framebuffer, framebuffer.Handle);
            g.Viewport(0, 0, 4, 4);
            g.UseProgram(program.Handle);
            g.BindBufferBase(BufferTargetARB.UniformBuffer, 3, tint.Handle);
            g.BindVertexArray(vertices.Handle);
            g.DrawArrays(PrimitiveType.Triangles, 0, 3);
            device.Check("fill");
            g.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            return target.Download<byte>();
        });
        for (int i = 0; i < pixels.Length; i += 4)
        {
            Assert.Equal(255, pixels[i]);
            Assert.Equal(0, pixels[i + 1]);
            Assert.InRange((int)pixels[i + 2], 127, 128);
            Assert.Equal(255, pixels[i + 3]);
        }
    }

    [SkippableFact]
    public void ACompileErrorNamesTheProgramAndShowsTheLine()
    {
        GpuException error = Assert.Throws<GpuException>(() => gl.Require().Run(device =>
            new GpuProgram(device, "broken", Sources(Fragment.Replace("color = tint;", "color = nothing;")))));
        Assert.Equal("broken", error.Resource);
        Assert.Contains("Fragment stage did not compile", error.Message);
        Assert.Contains("color = nothing;", error.Message);
    }

    [SkippableFact]
    public void ABlockTheProgramDoesNotDeclareIsAnError()
    {
        Assert.Throws<GpuException>(() => gl.Require().Run(device =>
        {
            using GpuProgram program = new(device, "fill", Sources(Fragment));
            program.BindUniformBlock("gsys_context", 0);
        }));
    }

    [SkippableFact]
    public void AProgramWithNoStagesIsAnError()
    {
        Assert.Throws<GpuException>(() => gl.Require().Run(device => new GpuProgram(device, "empty", new Dictionary<GpuShaderStage, string>())));
    }
}
