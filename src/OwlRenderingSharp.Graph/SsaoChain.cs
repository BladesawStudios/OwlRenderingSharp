using System.Runtime.InteropServices;
using OwlRenderingSharp.Gpu;

namespace OwlRenderingSharp.Graph;

/// <summary>The game's screen-space ambient occlusion: its alchemy detect compute program followed by its two-direction bilateral blur, run from the screen shader archive.</summary>
/// <remarks>
/// The detect program reads the hardware depth of the view and writes occlusion (one is open) in the first channel. Its parameter block is filled from
/// the engine's parameter names (<c>alchemy_radius</c>, <c>alchemy_max_radius</c>, <c>alchemy_bias</c>, <c>alchemy_detection_intensity</c>, <c>alchemy_density</c>, <c>alchemy_sample_pair_num</c>) as the
/// program uses them: the depth-to-distance terms, the view ray scales, the sample count and its spiral, the radius in view units and in screen units, the bias, the intensity and the contrast power.
/// The engine's default values for the radii are not readable here, so <see cref="SsaoSettings"/> carries this library's choices. The result is smoothed by the game's bilateral blur, vertical then horizontal (the second variant), whose taps are cut off by depth difference.
/// The result is owned by the caller and has the size of the depth image.
/// </remarks>
public sealed class SsaoChain : IDisposable
{
    private const string Archive = GamePostChain.Archive;
    private const int DetectBlock = 656, DynamicBlock = 16;

    private readonly RenderServices _services;
    private const string FlipSource = """
        #version 450 core
        layout (binding = 0) uniform sampler2D source;
        in vec2 uv;
        out vec4 color;
        void main()
        {
            ivec2 size = textureSize(source, 0);
            color = texelFetch(source, ivec2(int(gl_FragCoord.x), size.y - 1 - int(gl_FragCoord.y)), 0);
        }
        """;

    private readonly ShaderComputePass _detect, _vertical, _horizontal;
    private readonly GpuScreenPass _flip;

    public SsaoChain(RenderServices services)
    {
        _services = services;
        _detect = new ShaderComputePass(services, Archive, "ssao_compute_detect", 0, 16, 32);
        _vertical = new ShaderComputePass(services, Archive, "ssao_compute_bilateral", 0, 8, 16);
        _horizontal = new ShaderComputePass(services, Archive, "ssao_compute_bilateral", 1, 16, 8);
        _flip = new GpuScreenPass(services.Device, "ssao flip", FlipSource);
    }

    public GpuTexture Run(GpuTexture depth, Camera camera, SsaoSettings settings)
    {
        GpuDevice device = _services.Device;
        int width = depth.Width, height = depth.Height;
        using GpuTexture topDown = new(device, "ssao depth", GpuFormat.R32F, width, height);
        using GpuTexture raw = new(device, "ssao detected", GpuFormat.Rgba16F, width, height);
        using GpuTexture blurred = new(device, "ssao blurred", GpuFormat.Rgba16F, width, height);
        Flip(depth, topDown);
        byte[] dynamic = Bytes(DynamicBlock, width, height, 1f / width, 1f / height);
        _detect.Run(raw, new Dictionary<string, uint> { ["cSamplerDepth"] = topDown.Handle }, new Dictionary<string, byte[]> { ["Detect"] = DetectParams(camera, width, height, settings), ["Dynamic"] = dynamic });
        Dictionary<string, byte[]> blur = new() { ["Dynamic"] = dynamic, ["Bilateral"] = BilateralParams(settings) };
        _vertical.Run(blurred, new Dictionary<string, uint> { ["cSamplerColor"] = raw.Handle }, blur);
        _horizontal.Run(raw, new Dictionary<string, uint> { ["cSamplerColor"] = blurred.Handle }, blur);
        GpuTexture result = new(device, "ssao", GpuFormat.Rgba16F, width, height);
        Flip(raw, result);
        return result;
    }

    // The game's programs read images with the top row first; the frame's render targets have the bottom row first, so the depth goes in and the occlusion comes out flipped.
    private void Flip(GpuTexture source, GpuTexture target)
    {
        using GpuFramebuffer framebuffer = new(_services.Device, target.Format.ToString(), [target]);
        _flip.Run(framebuffer, [source.Handle]);
    }

    private static byte[] DetectParams(Camera camera, int width, int height, SsaoSettings s)
    {
        float[] v = new float[DetectBlock / 4];
        float tan = MathF.Tan(camera.FovRadians * 0.5f), aspect = (float)width / height;
        float near = camera.NearPlane, far = camera.FarPlane;
        int samples = s.SamplePairs * 2;
        v[0] = s.Radius * 0.5f / (tan * aspect);
        v[2] = s.MaxRadiusUv;
        v[3] = s.Bias;
        v[4] = s.Turns;
        v[5] = 2f * s.Radius * s.Intensity;
        v[6] = s.DetectionIntensity;
        v[7] = BitConverter.UInt32BitsToSingle((uint)samples);
        v[8] = 1f / samples;
        v[11] = far * 0.999f;
        v[18] = aspect;
        v[26] = 1f;
        v[12] = s.Contrast;
        v[36 * 4] = 1f / near;
        v[36 * 4 + 1] = -(1f - near / far) / near;
        v[38 * 4] = 1f;
        v[40 * 4] = -tan * aspect;
        v[40 * 4 + 1] = -tan;
        return MemoryMarshal.AsBytes<float>(v).ToArray();
    }

    // The bilateral blur: the weight of the centre and of each tap outward, the tap count as an unsigned integer in the first field of row eight, and how sharply a depth difference cuts a tap off.
    private static byte[] BilateralParams(SsaoSettings s)
    {
        float[] v = new float[36];
        for (int i = 0; i < s.BlurWeights.Count; i++) v[i * 4] = s.BlurWeights[i];
        v[32] = BitConverter.UInt32BitsToSingle((uint)s.BlurWeights.Count);
        v[33] = s.BlurDepthSharpness;
        return MemoryMarshal.AsBytes<float>(v).ToArray();
    }

    private static byte[] Bytes(int size, params float[] values)
    {
        byte[] bytes = new byte[Math.Max(size, values.Length * sizeof(float))];
        Buffer.BlockCopy(values, 0, bytes, 0, values.Length * sizeof(float));
        return bytes;
    }

    public void Dispose()
    {
        _flip.Dispose();
        _horizontal.Dispose();
        _vertical.Dispose();
        _detect.Dispose();
    }
}
