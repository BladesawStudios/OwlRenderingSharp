using System.Numerics;
using OwlRenderingSharp.Gpu;

namespace OwlRenderingSharp.Graph;

/// <summary>Distance fog: the lit image mixed toward a fog colour between a start and an end distance.</summary>
/// <remarks>
/// The place's palette gives <c>FogColor</c>, <c>FogStart</c> and <c>FogEnd</c> (in metres). The fraction is linear in the view distance between them and is scaled by the colour's alpha, so
/// a palette whose fog colour is zero, as in Kokiri Forest at noon, leaves the image as it is. The game's volumetric fog (<c>volume_fog</c>, froxel volumes) is not run; this is the distance term alone.
/// Pixels with no geometry are left as they are. It owns its program and is used from the GL thread only.
/// </remarks>
public sealed class DepthFogPass : IDisposable
{
    private const string Source = """
        #version 450 core
        layout (binding = 0) uniform sampler2D colorImage;
        layout (binding = 1) uniform sampler2D viewDepth;
        layout (std140, binding = 0) uniform Params { vec4 camera; vec4 fogColor; vec4 range; };
        in vec2 uv;
        out vec4 color;
        void main()
        {
            ivec2 pixel = ivec2(gl_FragCoord.xy);
            vec4 lit = texelFetch(colorImage, pixel, 0);
            float d = texelFetch(viewDepth, pixel, 0).r;
            if (d >= 1.0) { color = lit; return; }
            float z = camera.x / (1.0 - d * (1.0 - camera.x / camera.y));
            float t = clamp((z - range.x) / max(range.y - range.x, 1e-3), 0.0, 1.0) * fogColor.a;
            color = vec4(mix(lit.rgb, fogColor.rgb, t), lit.a);
        }
        """;

    private readonly GpuDevice _device;
    private readonly GpuScreenPass _pass;

    public DepthFogPass(GpuDevice device)
    {
        _device = device;
        _pass = new GpuScreenPass(device, "depth fog", Source);
    }

    public void Run(GpuTexture lit, GpuTexture viewDepth, Camera camera, Vector4 fogColor, float start, float end, GpuFramebuffer target)
    {
        using GpuBuffer parameters = new(_device, "depth fog parameters", 48);
        parameters.Write<float>([camera.NearPlane, camera.FarPlane, 0f, 0f, fogColor.X, fogColor.Y, fogColor.Z, fogColor.W, start, end, 0f, 0f]);
        _pass.Run(target, [lit.Handle, viewDepth.Handle], parameters);
    }

    public void Dispose() => _pass.Dispose();
}
