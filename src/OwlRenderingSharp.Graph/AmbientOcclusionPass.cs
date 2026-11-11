using OwlRenderingSharp.Gpu;

namespace OwlRenderingSharp.Graph;

/// <summary>Darkens a lit image where nearby geometry crowds a pixel, from the view depth image alone.</summary>
/// <remarks>
/// <see cref="Apply"/> instead multiplies an occlusion image (the game's, from <see cref="SsaoChain"/>) onto the lit image.
/// This stands in for the game's screen-space occlusion (the compute chain that detects, blurs and merges into the shadow prepass), whose parameter blocks are not decoded. For each pixel it
/// rebuilds the view-space distance from the hardware depth (near over distance, as the game's programs do), fits the plane of the surface through the pixel from the depth of its neighbours (so a tilted or gently curved surface does not occlude itself, which showed as overlapping circles on skin) and counts how many of a fixed ring of taps within <c>radius</c> metres lie in front of
/// that plane by more than a small bias that grows with the tap's distance from the pixel (the head's polygons and the eyes' are coarse, and a bias that grew more slowly let their facets occlude each other: a surface tilted by about 25 degrees or less no longer counts), range-checked so far-away geometry does not darken. The colour is multiplied by
/// <c>1 - strength * occlusion</c>. Pixels with no geometry are untouched, and so are skin pixels when the depth of the skin shapes is given: the head's polygons are too coarse for a depth-only occlusion, which printed their facets on the face, and the game's skin takes its occlusion from its textures. It owns its programs and a scratch image and is used from the GL thread only.
/// </remarks>
public sealed class AmbientOcclusionPass : IDisposable
{
    private const string Source = """
        #version 450 core
        layout (binding = 0) uniform sampler2D colorImage;
        layout (binding = 1) uniform sampler2D depthImage;
        layout (binding = 2) uniform sampler2D skinImage;
        layout (std140, binding = 0) uniform Params { vec4 camera; vec4 settings; };
        in vec2 uv;
        out vec4 color;
        float viewDistance(ivec2 p)
        {
            float d = texelFetch(depthImage, clamp(p, ivec2(0), textureSize(depthImage, 0) - 1), 0).r;
            return d >= 1.0 ? 1e9 : camera.x / (1.0 - d * (1.0 - camera.x / camera.y));
        }
        float slopeAlong(ivec2 p, ivec2 axis, float z)
        {
            float a = viewDistance(p + axis) - z, b = z - viewDistance(p - axis);
            return abs(a) < abs(b) ? a : b;
        }
        void main()
        {
            ivec2 pixel = ivec2(gl_FragCoord.xy);
            vec4 lit = texelFetch(colorImage, pixel, 0);
            float z = viewDistance(pixel);
            if (z > 1e8) { color = lit; return; }
            if (settings.w > 0.5)
            {
                float skin = texelFetch(skinImage, pixel, 0).r;
                if (skin < 1.0 && abs(skin - texelFetch(depthImage, pixel, 0).r) < 1e-6) { color = lit; return; }
            }
            float radiusPixels = settings.x * camera.z / z;
            vec2 slope = vec2(slopeAlong(pixel, ivec2(1, 0), z), slopeAlong(pixel, ivec2(0, 1), z));
            float occlusion = 0.0;
            const int taps = 16;
            for (int i = 0; i < taps; i++)
            {
                float angle = (float(i) + 0.5) * 2.399963;
                float r = sqrt((float(i) + 0.5) / float(taps)) * radiusPixels;
                vec2 offset = round(vec2(cos(angle), sin(angle)) * r);
                float sampleZ = viewDistance(pixel + ivec2(offset));
                float delta = z + dot(slope, offset) - sampleZ;
                float reach = length(offset) * z / camera.z;
                occlusion += (delta > settings.z + 0.45 * reach && delta < settings.x * 2.0) ? 1.0 : 0.0;
            }
            occlusion /= float(taps);
            color = vec4(lit.rgb * (1.0 - settings.y * occlusion), lit.a);
        }
        """;

    private const string ApplySource = """
        #version 450 core
        layout (binding = 0) uniform sampler2D colorImage;
        layout (binding = 1) uniform sampler2D occlusionImage;
        layout (std140, binding = 0) uniform Params { vec4 settings; };
        in vec2 uv;
        out vec4 color;
        void main()
        {
            vec4 lit = texelFetch(colorImage, ivec2(gl_FragCoord.xy), 0);
            float open = texelFetch(occlusionImage, ivec2(gl_FragCoord.xy), 0).r;
            color = vec4(lit.rgb * (1.0 - settings.x * (1.0 - open)), lit.a);
        }
        """;

    private readonly GpuDevice _device;
    private readonly GpuScreenPass _pass, _apply;

    public AmbientOcclusionPass(GpuDevice device)
    {
        _device = device;
        _pass = new GpuScreenPass(device, "ambient occlusion", Source);
        _apply = new GpuScreenPass(device, "ambient occlusion apply", ApplySource);
    }

    public void Run(GpuTexture lit, GpuTexture depth, Camera camera, float radius, float strength, GpuFramebuffer target, GpuTexture? skinDepth = null)
    {
        float focalPixels = lit.Height * 0.5f / MathF.Tan(camera.FovRadians * 0.5f);
        using GpuBuffer parameters = new(_device, "ambient occlusion parameters", 32);
        parameters.Write<float>([camera.NearPlane, camera.FarPlane, focalPixels, 0f, radius, strength, radius * 0.05f, skinDepth is null ? 0f : 1f]);
        _pass.Run(target, [lit.Handle, depth.Handle, (skinDepth ?? depth).Handle], parameters);
    }

    public void Apply(GpuTexture lit, GpuTexture occlusion, float strength, GpuFramebuffer target)
    {
        using GpuBuffer parameters = new(_device, "ambient occlusion apply parameters", 16);
        parameters.Write<float>([strength, 0f, 0f, 0f]);
        _apply.Run(target, [lit.Handle, occlusion.Handle], parameters);
    }

    public void Dispose()
    {
        _apply.Dispose();
        _pass.Dispose();
    }
}
