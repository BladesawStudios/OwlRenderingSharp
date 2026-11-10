using OwlRenderingSharp.Gpu;

namespace OwlRenderingSharp.Graph;

/// <summary>The two depth images of the view the shading programs read: <c>gsys_depth_buffer</c> and <c>gsys_normalized_linear_depth</c>.</summary>
/// <remarks>
/// <c>gsys_depth_buffer</c> is the hardware depth of the opaque shapes, zero to one with zero at the near plane: the programs turn it into <c>near / distance</c> with the context's
/// <c>1 - near / far</c>. <c>gsys_normalized_linear_depth</c> holds the view distance divided by the far plane in its first channel. Pixels with no geometry read one in both. The depth
/// texture given to <see cref="Resolve"/> must outlive the frame it is read in; the linear image and the samplers are released with this object.
/// </remarks>
public sealed class ViewDepthBuffers : IDisposable
{
    public const string DepthName = "gsys_depth_buffer", LinearName = "gsys_normalized_linear_depth";

    private const string Source = """
        #version 450 core
        layout (binding = 0) uniform sampler2D depthImage;
        layout (std140, binding = 0) uniform Params { vec4 nearFar; };
        in vec2 uv;
        out vec4 color;
        void main()
        {
            float d = texelFetch(depthImage, ivec2(gl_FragCoord.xy), 0).r;
            float distance = d >= 1.0 ? nearFar.y : nearFar.x / (1.0 - d * (1.0 - nearFar.x / nearFar.y));
            color = vec4(distance / nearFar.y, 0.0, 0.0, 1.0);
        }
        """;

    private readonly GpuDevice _device;
    private readonly GpuScreenPass _pass;
    private readonly GpuTexture _linear;
    private readonly GpuFramebuffer _target;
    private readonly GpuSampler _sampler;
    private FrameTexture _depth;

    public ViewDepthBuffers(GpuDevice device, int width, int height)
    {
        _device = device;
        _pass = new GpuScreenPass(device, "view depth", Source);
        _linear = new GpuTexture(device, "normalized linear depth", GpuFormat.R32F, width, height);
        _target = new GpuFramebuffer(device, "normalized linear depth", [_linear]);
        _sampler = new GpuSampler(device, "view depth", SamplerState.LinearClamp);
    }

    public IReadOnlyDictionary<string, FrameTexture> Textures => new Dictionary<string, FrameTexture>
    {
        [DepthName] = _depth,
        [LinearName] = new FrameTexture(_linear.Handle, _sampler.Handle),
    };

    /// <summary>The linear image as floats, one per pixel, for tools and tests.</summary>
    public float[] ReadLinear()
    {
        float[] rgba = _linear.DownloadAsFloats();
        return [.. Enumerable.Range(0, rgba.Length / 4).Select(i => rgba[i * 4])];
    }

    public void Resolve(GpuTexture depth, Camera camera)
    {
        _depth = new FrameTexture(depth.Handle, _sampler.Handle);
        using GpuBuffer parameters = new(_device, "view depth parameters", 16);
        parameters.Write<float>([camera.NearPlane, camera.FarPlane, 0f, 0f]);
        _pass.Run(_target, [depth.Handle], parameters);
    }

    public void Dispose()
    {
        _sampler.Dispose();
        _target.Dispose();
        _linear.Dispose();
        _pass.Dispose();
    }
}
