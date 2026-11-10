using OwlRenderingSharp.Gpu;

namespace OwlRenderingSharp.Graph;

/// <summary>Darkens a lit image where nearby geometry crowds a pixel, from the view depth image alone.</summary>
/// <remarks>
/// This stands in for the game's screen-space occlusion (the compute chain that detects, blurs and merges into the shadow prepass), whose parameter blocks are not decoded. For each pixel it
/// rebuilds the view-space position from the hardware depth (near over distance, as the game's programs do) and counts how many of a fixed ring of taps within <c>radius</c> metres lie in front of
/// the pixel's own surface by more than a small bias, range-checked so far-away geometry does not darken. The colour is multiplied by
/// <c>1 - strength * occlusion</c>. Pixels with no geometry are untouched. It owns its programs and a scratch image and is used from the GL thread only.
/// </remarks>
public sealed class AmbientOcclusionPass : IDisposable
{
    private const string Source = """
        #version 450 core
        layout (binding = 0) uniform sampler2D colorImage;
        layout (binding = 1) uniform sampler2D depthImage;
        layout (std140, binding = 0) uniform Params { vec4 camera; vec4 settings; };
        in vec2 uv;
        out vec4 color;
        float viewDistance(ivec2 p)
        {
            float d = texelFetch(depthImage, clamp(p, ivec2(0), textureSize(depthImage, 0) - 1), 0).r;
            return d >= 1.0 ? 1e9 : camera.x / (1.0 - d * (1.0 - camera.x / camera.y));
        }
        void main()
        {
            ivec2 pixel = ivec2(gl_FragCoord.xy);
            vec4 lit = texelFetch(colorImage, pixel, 0);
            float z = viewDistance(pixel);
            if (z > 1e8) { color = lit; return; }
            float radiusPixels = settings.x * camera.z / z;
            float occlusion = 0.0;
            const int taps = 16;
            for (int i = 0; i < taps; i++)
            {
                float angle = (float(i) + 0.5) * 2.399963;
                float r = sqrt((float(i) + 0.5) / float(taps)) * radiusPixels;
                float sampleZ = viewDistance(pixel + ivec2(round(vec2(cos(angle), sin(angle)) * r)));
                float delta = z - sampleZ;
                occlusion += (delta > settings.z && delta < settings.x * 2.0) ? 1.0 : 0.0;
            }
            occlusion /= float(taps);
            color = vec4(lit.rgb * (1.0 - settings.y * occlusion), lit.a);
        }
        """;

    private readonly GpuDevice _device;
    private readonly GpuScreenPass _pass;

    public AmbientOcclusionPass(GpuDevice device)
    {
        _device = device;
        _pass = new GpuScreenPass(device, "ambient occlusion", Source);
    }

    public void Run(GpuTexture lit, GpuTexture depth, Camera camera, float radius, float strength, GpuFramebuffer target)
    {
        float focalPixels = lit.Height * 0.5f / MathF.Tan(camera.FovRadians * 0.5f);
        using GpuBuffer parameters = new(_device, "ambient occlusion parameters", 32);
        parameters.Write<float>([camera.NearPlane, camera.FarPlane, focalPixels, 0f, radius, strength, radius * 0.05f, 0f]);
        _pass.Run(target, [lit.Handle, depth.Handle], parameters);
    }

    public void Dispose() => _pass.Dispose();
}
