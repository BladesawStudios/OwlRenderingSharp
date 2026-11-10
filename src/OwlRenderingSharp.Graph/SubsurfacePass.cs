using OwlRenderingSharp.Gpu;

namespace OwlRenderingSharp.Graph;

/// <summary>Screen-space subsurface scattering of skin: the lit image blurred across skin pixels, wider in red than in green and blue.</summary>
/// <remarks>
/// The game's own <c>skin_shading_scatter</c> program is an empty stub in the shader archive, so this is this library's separable screen-space scatter in the manner of Jimenez: a horizontal and a vertical
/// pass of thirteen taps whose reach is given in metres per colour channel and converted to pixels by the distance of the pixel. A tap contributes only if it is skin (its depth equals the depth image
/// of the skin shapes alone) and lies within the depth tolerance of the centre, so the blur does not bleed over silhouettes or onto cloth. The result is the lit image mixed
/// toward the blurred one by the strength. It owns its program and is used from the GL thread only.
/// </remarks>
public sealed class SubsurfacePass : IDisposable
{
    /// <summary>The scatter: widths are gaussian standard deviations in metres for red, green and blue.</summary>
    public sealed record Settings(float WidthRed = 0.004f, float WidthGreen = 0.002f, float WidthBlue = 0.001f, float Strength = 0.5f, float DepthTolerance = 0.02f);

    private const string Source = """
        #version 450 core
        layout (binding = 0) uniform sampler2D colorImage;
        layout (binding = 1) uniform sampler2D viewDepth;
        layout (binding = 2) uniform sampler2D skinDepth;
        layout (binding = 3) uniform sampler2D originalImage;
        layout (std140, binding = 0) uniform Params { vec4 camera; vec4 widths; vec4 direction; };
        in vec2 uv;
        out vec4 color;
        float distanceAt(sampler2D image, ivec2 p)
        {
            float d = texelFetch(image, clamp(p, ivec2(0), textureSize(image, 0) - 1), 0).r;
            return d >= 1.0 ? 1e9 : camera.x / (1.0 - d * (1.0 - camera.x / camera.y));
        }
        bool isSkin(ivec2 p)
        {
            float s = distanceAt(skinDepth, p);
            return s < 1e8 && abs(s - distanceAt(viewDepth, p)) < 1e-4 * s;
        }
        void main()
        {
            ivec2 pixel = ivec2(gl_FragCoord.xy);
            vec4 lit = texelFetch(colorImage, pixel, 0);
            if (!isSkin(pixel)) { color = lit; return; }
            float z = distanceAt(viewDepth, pixel);
            vec3 sigma = widths.xyz * camera.z / z;
            float reach = max(sigma.x, 0.5) * 3.0;
            float step = reach / 6.0;
            vec3 sum = vec3(0.0), weightSum = vec3(0.0);
            for (int i = -6; i <= 6; i++)
            {
                ivec2 p = pixel + ivec2(round(direction.xy * float(i) * step));
                if (!isSkin(p) || abs(distanceAt(viewDepth, p) - z) > widths.w) continue;
                vec3 w = exp(-0.5 * pow(float(i) * step / max(sigma, vec3(0.3)), vec3(2.0)));
                sum += w * texelFetch(colorImage, clamp(p, ivec2(0), textureSize(colorImage, 0) - 1), 0).rgb;
                weightSum += w;
            }
            vec3 blurred = sum / max(weightSum, vec3(1e-4));
            bool last = direction.z > 0.5;
            vec3 base = last ? texelFetch(originalImage, pixel, 0).rgb : lit.rgb;
            color = vec4(mix(base, blurred, last ? camera.w : 1.0), lit.a);
        }
        """;

    private readonly GpuDevice _device;
    private readonly GpuScreenPass _pass;

    public SubsurfacePass(GpuDevice device)
    {
        _device = device;
        _pass = new GpuScreenPass(device, "subsurface", Source);
    }

    public void Run(GpuTexture lit, GpuTexture viewDepth, GpuTexture skinDepth, Camera camera, Settings settings, GpuFramebuffer target)
    {
        float focalPixels = lit.Height * 0.5f / MathF.Tan(camera.FovRadians * 0.5f);
        using GpuTexture scratch = new(_device, "subsurface scratch", lit.Format, lit.Width, lit.Height);
        using (GpuFramebuffer horizontal = new(_device, "subsurface scratch", [scratch]))
            Pass(lit, viewDepth, skinDepth, lit, camera, settings, focalPixels, 1f, 0f, 0f, horizontal);
        Pass(scratch, viewDepth, skinDepth, lit, camera, settings, focalPixels, 0f, 1f, 1f, target);
    }

    private void Pass(GpuTexture source, GpuTexture viewDepth, GpuTexture skinDepth, GpuTexture original, Camera camera, Settings s, float focal, float dx, float dy, float last, GpuFramebuffer target)
    {
        using GpuBuffer parameters = new(_device, "subsurface parameters", 48);
        parameters.Write<float>([camera.NearPlane, camera.FarPlane, focal, s.Strength, s.WidthRed, s.WidthGreen, s.WidthBlue, s.DepthTolerance, dx, dy, last, 0f]);
        _pass.Run(target, [source.Handle, viewDepth.Handle, skinDepth.Handle, original.Handle], parameters);
    }

    public void Dispose() => _pass.Dispose();
}
