using OwlRenderingSharp.Gpu;

namespace OwlRenderingSharp.Graph;

/// <summary>Depth of field: pixels farther from the focal distance than its range are blurred by a disc whose radius grows with the difference.</summary>
/// <remarks>
/// The game's screen archive in this build has no depth-of-field program, so this is this library's gather blur. Distances within <c>range</c> metres of <c>focalDistance</c> are sharp; beyond that the
/// blur radius rises to <c>maxRadius</c> pixels over another <c>range</c> metres. A tap counts only if its own blur is at least as large as its distance from the centre, so a sharp foreground
/// does not bleed into a blurred background. Pixels with no geometry take the largest blur. The Kokiri Forest values of the game's palette (focal distance 50, range 50) keep a lone character sharp.
/// It owns its program and is used from the GL thread only.
/// </remarks>
public sealed class DepthOfFieldPass : IDisposable
{
    private const string Source = """
        #version 450 core
        layout (binding = 0) uniform sampler2D colorImage;
        layout (binding = 1) uniform sampler2D viewDepth;
        layout (std140, binding = 0) uniform Params { vec4 camera; vec4 focus; };
        in vec2 uv;
        out vec4 color;
        float blurAt(ivec2 p)
        {
            float d = texelFetch(viewDepth, clamp(p, ivec2(0), textureSize(viewDepth, 0) - 1), 0).r;
            float z = d >= 1.0 ? camera.y : camera.x / (1.0 - d * (1.0 - camera.x / camera.y));
            return focus.z * clamp((abs(z - focus.x) - focus.y) / max(focus.y, 1e-3), 0.0, 1.0);
        }
        void main()
        {
            ivec2 pixel = ivec2(gl_FragCoord.xy);
            float radius = blurAt(pixel);
            vec4 centre = texelFetch(colorImage, pixel, 0);
            if (radius < 0.5) { color = centre; return; }
            vec3 sum = centre.rgb;
            float weight = 1.0;
            const int taps = 24;
            for (int i = 0; i < taps; i++)
            {
                float angle = float(i) * 2.399963;
                float r = sqrt((float(i) + 0.5) / float(taps)) * radius;
                ivec2 p = pixel + ivec2(round(vec2(cos(angle), sin(angle)) * r));
                if (blurAt(p) < r) continue;
                sum += texelFetch(colorImage, clamp(p, ivec2(0), textureSize(colorImage, 0) - 1), 0).rgb;
                weight += 1.0;
            }
            color = vec4(sum / weight, centre.a);
        }
        """;

    private readonly GpuDevice _device;
    private readonly GpuScreenPass _pass;

    public DepthOfFieldPass(GpuDevice device)
    {
        _device = device;
        _pass = new GpuScreenPass(device, "depth of field", Source);
    }

    public void Run(GpuTexture lit, GpuTexture viewDepth, Camera camera, float focalDistance, float range, float maxRadius, GpuFramebuffer target)
    {
        using GpuBuffer parameters = new(_device, "depth of field parameters", 32);
        parameters.Write<float>([camera.NearPlane, camera.FarPlane, 0f, 0f, focalDistance, range, maxRadius, 0f]);
        _pass.Run(target, [lit.Handle, viewDepth.Handle], parameters);
    }

    public void Dispose() => _pass.Dispose();
}
