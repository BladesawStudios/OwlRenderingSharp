using OwlRenderingSharp.Gpu;

namespace OwlRenderingSharp.Graph;

/// <summary>Builds the bloom image of a linear HDR frame: the bright part at a quarter of the size, blurred.</summary>
/// <remarks>
/// The bright pass averages each four-by-four block of the source after the exposure, clamps each sample to the bloom clamp and keeps the share of it above the threshold; two separable
/// nine-tap blurs then spread it. The result is returned as a texture the caller owns and disposes. It is used from the GL thread only and releases its programs with it.
/// </remarks>
public sealed class BloomPass(GpuDevice device) : IDisposable
{
    private const string BrightSource = """
        #version 450 core
        layout (binding = 0) uniform sampler2D hdr;
        layout (std140, binding = 0) uniform Params { vec4 p; };
        in vec2 uv;
        out vec4 color;
        void main()
        {
            ivec2 origin = ivec2(gl_FragCoord.xy) * 4;
            vec3 sum = vec3(0.0);
            for (int j = 0; j < 4; j++)
                for (int i = 0; i < 4; i++)
                {
                    vec3 c = min(max(texelFetch(hdr, origin + ivec2(i, j), 0).rgb, 0.0) * p.x, vec3(p.z));
                    float l = max(c.r, max(c.g, c.b));
                    sum += c * (max(l - p.y, 0.0) / max(l, 1e-4));
                }
            color = vec4(sum / 16.0, 1.0);
        }
        """;

    private const string BlurSource = """
        #version 450 core
        layout (binding = 0) uniform sampler2D image;
        layout (std140, binding = 0) uniform Params { vec4 direction; };
        in vec2 uv;
        out vec4 color;
        void main()
        {
            const float w[5] = float[](0.2270270, 0.1945946, 0.1216216, 0.0540541, 0.0162162);
            ivec2 size = textureSize(image, 0);
            ivec2 p = ivec2(gl_FragCoord.xy);
            vec3 sum = texelFetch(image, p, 0).rgb * w[0];
            for (int k = 1; k < 5; k++)
            {
                ivec2 o = ivec2(direction.xy) * k;
                sum += texelFetch(image, clamp(p + o, ivec2(0), size - 1), 0).rgb * w[k];
                sum += texelFetch(image, clamp(p - o, ivec2(0), size - 1), 0).rgb * w[k];
            }
            color = vec4(sum, 1.0);
        }
        """;

    private readonly GpuScreenPass _bright = new(device, "bloom bright", BrightSource);
    private readonly GpuScreenPass _blur = new(device, "bloom blur", BlurSource);

    public GpuTexture Run(GpuTexture hdr, PostSettings settings)
    {
        int width = Math.Max(1, hdr.Width / 4), height = Math.Max(1, hdr.Height / 4);
        GpuTexture result = new(device, "bloom", GpuFormat.Rgba16F, width, height);
        using GpuTexture bright = new(device, "bloom bright", GpuFormat.Rgba16F, width, height);
        using GpuFramebuffer brightTarget = new(device, "bloom bright", [bright]);
        using GpuFramebuffer resultTarget = new(device, "bloom", [result]);
        using (GpuBuffer parameters = new(device, "bloom bright parameters", 16))
        {
            parameters.Write<float>([settings.Exposure, settings.BloomThreshold, settings.BloomClamp, 0f]);
            _bright.Run(brightTarget, [hdr], parameters);
        }

        using GpuTexture across = new(device, "bloom across", GpuFormat.Rgba16F, width, height);
        using GpuFramebuffer acrossTarget = new(device, "bloom across", [across]);
        Blur(bright, acrossTarget, 1f, 0f);
        Blur(across, resultTarget, 0f, 1f);
        return result;
    }

    private void Blur(GpuTexture source, GpuFramebuffer target, float x, float y)
    {
        using GpuBuffer parameters = new(device, "bloom blur parameters", 16);
        parameters.Write<float>([x, y, 0f, 0f]);
        _blur.Run(target, [source], parameters);
    }

    public void Dispose()
    {
        _blur.Dispose();
        _bright.Dispose();
    }
}
