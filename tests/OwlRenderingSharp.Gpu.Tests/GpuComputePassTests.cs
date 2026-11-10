namespace OwlRenderingSharp.Gpu.Tests;

[Collection(GlCollection.Name)]
public class GpuComputePassTests(GlFixture gl)
{
    private const string Fill = "#version 450 core\nlayout (local_size_x = 4, local_size_y = 4) in;\nlayout (rgba8, binding = 0) writeonly uniform image2D outImage;\nvoid main() { imageStore(outImage, ivec2(gl_GlobalInvocationID.xy), vec4(1.0, 0.0, 0.0, 1.0)); }\n";

    [SkippableFact]
    public void ADispatchWritesEveryPixelOfTheImageItCovers()
    {
        byte[] pixels = gl.Require().Run(device =>
        {
            using GpuProgram program = new(device, "fill", new Dictionary<GpuShaderStage, string> { [GpuShaderStage.Compute] = Fill });
            using GpuComputePass pass = new(device, "fill", program);
            using GpuTexture image = new(device, "image", GpuFormat.Rgba8, 8, 8);
            pass.Run(2, 2, [], [(0u, image)], []);
            return image.Download<byte>();
        });
        for (int i = 0; i < pixels.Length; i += 4) Assert.Equal([255, 0, 0, 255], pixels[i..(i + 4)]);
    }

    [SkippableFact]
    public void ADispatchOfNoGroupsIsRefusedByTheNameOfThePass()
    {
        GpuException error = Assert.Throws<GpuException>(() => gl.Require().Run(device =>
        {
            using GpuProgram program = new(device, "fill", new Dictionary<GpuShaderStage, string> { [GpuShaderStage.Compute] = Fill });
            using GpuComputePass pass = new(device, "empty dispatch", program);
            pass.Run(0, 1, [], [], []);
        }));
        Assert.Equal("empty dispatch", error.Resource);
    }
}
