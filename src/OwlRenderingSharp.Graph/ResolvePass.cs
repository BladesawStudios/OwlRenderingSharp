using OwlRenderingSharp.Gpu;

namespace OwlRenderingSharp.Graph;

/// <summary>Tone maps a linear HDR image and averages each square of samples down to one pixel: the resolve of a supersampled frame.</summary>
/// <remarks>
/// Each sample is tone mapped (the same curve and sRGB transfer as <see cref="FilmicTonemap"/>, with the exposure) before the squares are averaged, so one very bright sample cannot swamp its neighbours
/// the way averaging linear light would. The source must be exactly <c>factor</c> times the size of the target. A factor of one is a plain tone map. The output is 8-bit sRGB.
/// </remarks>
public sealed class ResolvePass(GpuDevice device) : IDisposable
{
    private const string Source = """
        #version 450 core
        layout (binding = 0) uniform sampler2D hdr;
        layout (std140, binding = 0) uniform Params { vec4 p; };
        in vec2 uv;
        out vec4 color;
        vec3 curve(vec3 x) { return clamp(x * (2.51 * x + 0.03) / (x * (2.43 * x + 0.59) + 0.14), 0.0, 1.0); }
        vec3 srgb(vec3 l) { return mix(l * 12.92, 1.055 * pow(l, vec3(1.0 / 2.4)) - 0.055, step(0.0031308, l)); }
        void main()
        {
            int factor = int(p.y);
            ivec2 origin = ivec2(gl_FragCoord.xy) * factor;
            vec4 sum = vec4(0.0);
            for (int j = 0; j < factor; j++)
                for (int i = 0; i < factor; i++)
                {
                    vec4 s = texelFetch(hdr, origin + ivec2(i, j), 0);
                    sum += vec4(srgb(curve(max(s.rgb, 0.0) * p.x)), clamp(s.a, 0.0, 1.0));
                }
            color = sum / float(factor * factor);
        }
        """;

    private readonly GpuScreenPass _pass = new(device, "resolve", Source);

    public void Run(GpuTexture hdr, int factor, float exposure, GpuFramebuffer target)
    {
        if (factor < 1 || hdr.Width != target.Width * factor || hdr.Height != target.Height * factor)
            throw new GpuException($"A resolve by {factor} needs a source {factor} times the target, not {hdr.Width}x{hdr.Height} to {target.Width}x{target.Height}", resource: "resolve");
        using GpuBuffer parameters = new(device, "resolve parameters", 16);
        parameters.Write<float>([exposure, factor, 0f, 0f]);
        _pass.Run(target, [hdr], parameters);
    }

    public void Dispose() => _pass.Dispose();
}
