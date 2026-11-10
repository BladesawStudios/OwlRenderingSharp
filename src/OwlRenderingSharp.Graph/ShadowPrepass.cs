using System.Numerics;
using OwlRenderingSharp.Gpu;

namespace OwlRenderingSharp.Graph;

/// <summary>The screen-space shadow mask the shading programs read as <c>gsys_shadow_prepass</c>, resolved from a depth image of the view and a <see cref="ShadowMap"/>.</summary>
/// <remarks>
/// For every pixel it rebuilds the world position from the depth, picks the cascade by view distance, and averages a 5x5 block of depth comparisons against that cascade. The result is laid out as the game's merge program writes it
/// (<c>shadow_pre_pass_merge</c> with a depth shadow and an occlusion input): red is the screen-space occlusion image's value (one where open, or one with none given), green is the sun visibility (1 is lit, 0 is shadowed), and the last two are one. Pixels with no geometry are lit. The mask has the size it is created with and is released with this object.
/// </remarks>
public sealed class ShadowPrepass : IDisposable
{
    public const string SamplerName = "gsys_shadow_prepass";
    private const int MaxCascades = 3;

    private const string Source = """
        #version 450 core
        layout (binding = 0) uniform sampler2D depthImage;
        layout (binding = 1) uniform sampler2DArray cascades;
        layout (binding = 2) uniform sampler2D occlusionImage;
        layout (std140, binding = 0) uniform Params
        {
            mat4 inverseViewProjection;
            mat4 toShadow[3];
            vec4 splits;
            vec4 eye;
            vec4 forward;
            vec4 settings;
        };
        in vec2 uv;
        out vec4 color;
        void main()
        {
            ivec2 pixel = ivec2(gl_FragCoord.xy);
            float depth = texelFetch(depthImage, pixel, 0).r;
            float ao = settings.w > 0.0 ? 1.0 - settings.w * (1.0 - texelFetch(occlusionImage, pixel, 0).r) : 1.0;
            if (depth >= 1.0) { color = vec4(1.0); return; }
            if (settings.z < 0.5) { color = vec4(ao, 1.0, 1.0, 1.0); return; }
            vec2 ndc = (gl_FragCoord.xy / vec2(textureSize(depthImage, 0))) * 2.0 - 1.0;
            vec4 w = inverseViewProjection * vec4(ndc, depth, 1.0);
            vec3 world = w.xyz / w.w;
            float viewDepth = dot(world - eye.xyz, forward.xyz);
            int k = viewDepth < splits.x ? 0 : (viewDepth < splits.y ? 1 : 2);
            k = min(k, int(settings.z) - 1);
            vec4 t = toShadow[k] * vec4(world, 1.0);
            float res = settings.y;
            if (any(lessThan(t.xy, vec2(0.0))) || any(greaterThan(t.xy, vec2(1.0)))) { color = vec4(ao, 1.0, 1.0, 1.0); return; }
            ivec2 texel = ivec2(t.xy * res);
            float lit = 0.0;
            for (int j = -2; j <= 2; j++)
                for (int i = -2; i <= 2; i++)
                {
                    ivec2 p = clamp(texel + ivec2(i, j), ivec2(0), ivec2(int(res) - 1));
                    lit += (t.z - settings.x <= texelFetch(cascades, ivec3(p, k), 0).r) ? 1.0 : 0.0;
                }
            lit /= 25.0;
            color = vec4(ao, lit, 1.0, 1.0);
        }
        """;

    private readonly GpuDevice _device;
    private readonly GpuScreenPass _pass;
    private readonly GpuTexture _mask;
    private readonly GpuFramebuffer _target;
    private readonly GpuSampler _sampler;

    public ShadowPrepass(GpuDevice device, int width, int height)
    {
        _device = device;
        _pass = new GpuScreenPass(device, "shadow prepass", Source);
        _mask = new GpuTexture(device, "shadow prepass mask", GpuFormat.Rgba8, width, height);
        _target = new GpuFramebuffer(device, "shadow prepass mask", [_mask]);
        _sampler = new GpuSampler(device, "shadow prepass mask", SamplerState.LinearClamp);
        Texture = new FrameTexture(_mask.Handle, _sampler.Handle);
    }

    public FrameTexture Texture { get; }

    /// <summary>The mask as bytes, four per pixel, for tools and tests.</summary>
    public byte[] ReadMask() => _mask.Download<byte>();

    public void Resolve(GpuTexture depth, Camera camera, ShadowMap shadows, float bias = 0.0015f, GpuTexture? occlusion = null, float occlusionStrength = 1f) =>
        Resolve(depth, camera, shadows.Cascades, shadows.DepthHandle, shadows.Resolution, bias, occlusion, occlusionStrength);

    /// <summary>Resolves the mask with no shadow map: everything is lit and only the occlusion shows.</summary>
    public void Resolve(GpuTexture depth, Camera camera, GpuTexture occlusion, float occlusionStrength = 1f) => Resolve(depth, camera, [], 0, 1, 0f, occlusion, occlusionStrength);

    public void Resolve(GpuTexture depth, Camera camera, IReadOnlyList<ShadowCascade> cascades, uint cascadeDepthArray, int resolution, float bias = 0.0015f, GpuTexture? occlusion = null, float occlusionStrength = 1f)
    {
        if (cascades.Count > MaxCascades) throw new GpuException($"A shadow prepass reads one to {MaxCascades} cascades, not {cascades.Count}", resource: "shadow prepass");
        Matrix4x4.Invert(camera.ViewMatrix() * camera.ProjectionMatrix((float)depth.Width / depth.Height), out Matrix4x4 inverse);
        float[] data = new float[16 + 48 + 16];
        Write(data, 0, inverse);
        for (int i = 0; i < MaxCascades; i++) Write(data, 16 + i * 16, cascades.Count == 0 ? Matrix4x4.Identity : cascades[Math.Min(i, cascades.Count - 1)].TextureMatrix);
        Vector3 forward = Vector3.Normalize(camera.Target - camera.Eye);
        float[] tail = [cascades.Count > 0 ? cascades[0].Split : float.MaxValue, cascades.Count > 1 ? cascades[1].Split : float.MaxValue, 0f, 0f, camera.Eye.X, camera.Eye.Y, camera.Eye.Z, 0f, forward.X, forward.Y, forward.Z, 0f, bias, resolution, cascades.Count, occlusion is null ? 0f : occlusionStrength];
        tail.CopyTo(data, 64);
        using GpuBuffer parameters = new(_device, "shadow prepass parameters", data.Length * sizeof(float));
        parameters.Write<float>(data);
        _pass.Run(_target, [depth.Handle, cascadeDepthArray, occlusion?.Handle ?? 0], parameters);
    }

    // A Matrix4x4 is row-major with row-vector maths; the same sixteen floats read as a GLSL mat4 are its transpose, which multiplies a column vector the same way.
    private static void Write(float[] data, int offset, Matrix4x4 m)
    {
        float[] values = [m.M11, m.M12, m.M13, m.M14, m.M21, m.M22, m.M23, m.M24, m.M31, m.M32, m.M33, m.M34, m.M41, m.M42, m.M43, m.M44];
        values.CopyTo(data, offset);
    }

    public void Dispose()
    {
        _sampler.Dispose();
        _target.Dispose();
        _mask.Dispose();
        _pass.Dispose();
    }
}
