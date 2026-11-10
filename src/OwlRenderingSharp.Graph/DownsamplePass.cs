using OwlRenderingSharp.Gpu;

namespace OwlRenderingSharp.Graph;

/// <summary>Averages each square of samples of a display-linear image down to one pixel and encodes it as sRGB: the resolve of a supersampled frame the post chain has already tone mapped.</summary>
/// <remarks>
/// Each sample is encoded before the squares are averaged, as <see cref="ResolvePass"/> does, so one very bright sample cannot swamp its neighbours. The source must be exactly <c>factor</c> times the size of the target.
/// </remarks>
public sealed class DownsamplePass(GpuDevice device) : IDisposable
{
    private const string Source = """
        #version 450 core
        layout (binding = 0) uniform sampler2D image;
        layout (std140, binding = 0) uniform Params { vec4 p; };
        in vec2 uv;
        out vec4 color;
        vec3 srgb(vec3 l) { return mix(l * 12.92, 1.055 * pow(l, vec3(1.0 / 2.4)) - 0.055, step(0.0031308, l)); }
        void main()
        {
            int factor = int(p.x);
            ivec2 origin = ivec2(gl_FragCoord.xy) * factor;
            vec4 sum = vec4(0.0);
            for (int j = 0; j < factor; j++)
                for (int i = 0; i < factor; i++)
                {
                    vec4 s = texelFetch(image, origin + ivec2(i, j), 0);
                    sum += vec4(srgb(clamp(s.rgb, 0.0, 1.0)), clamp(s.a, 0.0, 1.0));
                }
            color = sum / float(factor * factor);
        }
        """;

    private readonly GpuScreenPass _pass = new(device, "downsample", Source);

    public void Run(GpuTexture image, int factor, GpuFramebuffer target)
    {
        if (factor < 1 || image.Width != target.Width * factor || image.Height != target.Height * factor)
            throw new GpuException($"A downsample by {factor} needs a source {factor} times the target, not {image.Width}x{image.Height} to {target.Width}x{target.Height}", resource: "downsample");
        using GpuBuffer parameters = new(device, "downsample parameters", 16);
        parameters.Write<float>([factor, 0f, 0f, 0f]);
        _pass.Run(target, [image], parameters);
    }

    public void Dispose() => _pass.Dispose();
}
