using OwlRenderingSharp.Gpu;

namespace OwlRenderingSharp.Graph;

/// <summary>Fast approximate anti-aliasing of an 8-bit sRGB image: it blurs along edges that the luma of the neighbouring pixels reveals.</summary>
/// <remarks>
/// This is the compact FXAA of Timothy Lottes: four diagonal neighbours give an edge direction, two or four taps along it are averaged, and the wider average is kept only when it stays inside the
/// local luma range. It works on the colour as stored (gamma encoded), keeps the alpha of the centre pixel and does nothing to flat areas.
/// </remarks>
public sealed class FxaaPass(GpuDevice device) : IDisposable
{
    private const string Source = """
        #version 450 core
        layout (binding = 0) uniform sampler2D image;
        in vec2 uv;
        out vec4 color;
        float luma(vec3 c) { return dot(c, vec3(0.299, 0.587, 0.114)); }
        void main()
        {
            vec2 px = 1.0 / vec2(textureSize(image, 0));
            vec4 centre = texture(image, uv);
            vec3 nw = texture(image, uv + vec2(-1.0, -1.0) * px).rgb;
            vec3 ne = texture(image, uv + vec2(1.0, -1.0) * px).rgb;
            vec3 sw = texture(image, uv + vec2(-1.0, 1.0) * px).rgb;
            vec3 se = texture(image, uv + vec2(1.0, 1.0) * px).rgb;
            float lNW = luma(nw), lNE = luma(ne), lSW = luma(sw), lSE = luma(se), lM = luma(centre.rgb);
            float lMin = min(lM, min(min(lNW, lNE), min(lSW, lSE)));
            float lMax = max(lM, max(max(lNW, lNE), max(lSW, lSE)));
            vec2 dir = vec2(-((lNW + lNE) - (lSW + lSE)), (lNW + lSW) - (lNE + lSE));
            float reduce = max((lNW + lNE + lSW + lSE) * 0.25 * (1.0 / 8.0), 1.0 / 128.0);
            float rcp = 1.0 / (min(abs(dir.x), abs(dir.y)) + reduce);
            dir = clamp(dir * rcp, vec2(-8.0), vec2(8.0)) * px;
            vec3 a = 0.5 * (texture(image, uv + dir * (1.0 / 3.0 - 0.5)).rgb + texture(image, uv + dir * (2.0 / 3.0 - 0.5)).rgb);
            vec3 b = a * 0.5 + 0.25 * (texture(image, uv + dir * -0.5).rgb + texture(image, uv + dir * 0.5).rgb);
            float lB = luma(b);
            color = vec4((lB < lMin || lB > lMax) ? a : b, centre.a);
        }
        """;

    private readonly GpuScreenPass _pass = new(device, "fxaa", Source);

    public void Run(GpuTexture source, GpuFramebuffer target) => _pass.Run(target, [source]);

    public void Dispose() => _pass.Dispose();
}
