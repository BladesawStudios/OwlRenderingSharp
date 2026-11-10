using OwlRenderingSharp.Gpu;

namespace OwlRenderingSharp.Graph;

/// <summary>Gives a snapshot a transparent background: the share of each output pixel the model covers becomes its alpha.</summary>
/// <remarks>
/// <see cref="Mask"/> writes one where the depth image of the view has geometry and zero where it has none, at the size of the supersampled draw. <see cref="Apply"/> averages the mask over the
/// supersampling block of each output pixel for its alpha, and divides the colour (drawn over black, so premultiplied) by that alpha to give straight colour, so edges carry no dark fringe.
/// Surfaces that are only alpha blended and not in the depth prepass (they leave no depth) count as empty. It owns its programs and is used from the GL thread only.
/// </remarks>
public sealed class CoverageAlphaPass : IDisposable
{
    private const string MaskSource = """
        #version 450 core
        layout (binding = 0) uniform sampler2D depthImage;
        in vec2 uv;
        out vec4 color;
        void main()
        {
            color = vec4(texelFetch(depthImage, ivec2(gl_FragCoord.xy), 0).r < 1.0 ? 1.0 : 0.0);
        }
        """;

    private const string ApplySource = """
        #version 450 core
        layout (binding = 0) uniform sampler2D colorImage;
        layout (binding = 1) uniform sampler2D maskImage;
        layout (std140, binding = 0) uniform Params { vec4 settings; };
        in vec2 uv;
        out vec4 color;
        void main()
        {
            ivec2 pixel = ivec2(gl_FragCoord.xy);
            int factor = int(settings.x);
            float covered = 0.0;
            for (int j = 0; j < factor; j++)
                for (int i = 0; i < factor; i++)
                    covered += texelFetch(maskImage, pixel * factor + ivec2(i, j), 0).r;
            float alpha = covered / float(factor * factor);
            vec3 rgb = texelFetch(colorImage, pixel, 0).rgb;
            color = vec4(alpha > 0.0 ? min(rgb / alpha, vec3(1.0)) : vec3(0.0), alpha);
        }
        """;

    private readonly GpuDevice _device;
    private readonly GpuScreenPass _mask, _apply;

    public CoverageAlphaPass(GpuDevice device)
    {
        _device = device;
        _mask = new GpuScreenPass(device, "coverage mask", MaskSource);
        _apply = new GpuScreenPass(device, "coverage alpha", ApplySource);
    }

    public void Mask(GpuTexture viewDepth, GpuFramebuffer target) => _mask.Run(target, [viewDepth.Handle]);

    public void Apply(GpuTexture color, GpuTexture mask, int factor, GpuFramebuffer target)
    {
        using GpuBuffer parameters = new(_device, "coverage alpha parameters", 16);
        parameters.Write<float>([factor, 0f, 0f, 0f]);
        _apply.Run(target, [color.Handle, mask.Handle], parameters);
    }

    public void Dispose()
    {
        _apply.Dispose();
        _mask.Dispose();
    }
}
