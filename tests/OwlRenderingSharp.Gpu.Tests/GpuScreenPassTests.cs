namespace OwlRenderingSharp.Gpu.Tests;

[Collection(GlCollection.Name)]
public class GpuScreenPassTests(GlFixture gl)
{
    private const string Invert = "#version 450 core\nlayout (binding = 0) uniform sampler2D src;\nin vec2 uv;\nout vec4 color;\nvoid main()\n{\n    color = vec4(1.0 - texture(src, uv).rgb, 1.0);\n}\n";

    [SkippableFact]
    public void APassDrawsItsFragmentProgramOverTheWholeTargetFromItsInputs()
    {
        byte[] pixels = gl.Require().Run(device =>
        {
            using GpuScreenPass pass = new(device, "invert", Invert);
            using GpuTexture source = new(device, "source", GpuFormat.Rgba8, 2, 2);
            source.Upload<byte>([255, 0, 0, 255, 255, 0, 0, 255, 255, 0, 0, 255, 255, 0, 0, 255]);
            using GpuTexture target = new(device, "target", GpuFormat.Rgba8, 4, 4);
            using GpuFramebuffer framebuffer = new(device, "target", [target]);
            pass.Run(framebuffer, [source]);
            return target.Download<byte>();
        });
        for (int i = 0; i < pixels.Length; i += 4) Assert.Equal([0, 255, 255, 255], pixels[i..(i + 4)]);
    }

    [SkippableFact]
    public void ABrokenFragmentProgramIsRefusedByTheNameOfThePass()
    {
        GpuException error = Assert.Throws<GpuException>(() => gl.Require().Run(device => new GpuScreenPass(device, "broken pass", "#version 450 core\nvoid main() { nothing(); }\n")));
        Assert.Equal("broken pass", error.Resource);
    }
}
