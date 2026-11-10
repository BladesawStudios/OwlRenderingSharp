namespace OwlRenderingSharp.Graph;

/// <summary>The inputs of the game's alchemy occlusion, named for the engine parameters they stand for.</summary>
/// <param name="Radius">World radius of the sample ring in metres (<c>alchemy_radius</c>).</param>
/// <param name="MaxRadiusUv">The largest screen radius in texture units (<c>alchemy_max_radius</c>).</param>
/// <param name="Bias">How far in front of the surface's tangent plane a sample must lie to occlude, in metres (<c>alchemy_bias</c>).</param>
/// <param name="SamplePairs">Sample pairs (<c>alchemy_sample_pair_num</c>; the engine's default is five).</param>
/// <param name="Turns">Turns of the sample spiral.</param>
/// <param name="Intensity">Scale of the summed occlusion.</param>
/// <param name="DetectionIntensity">Mix of the occlusion into the result (<c>alchemy_detection_intensity</c>, default one).</param>
/// <param name="Weights">Blur weights, centre first; the default is a gaussian of about two texels.</param>
/// <param name="BlurDepthSharpness">How sharply a relative depth difference cuts a blur tap off (<c>alchemy_bilateral_depth_threshold</c>, default 30).</param>
/// <param name="Contrast">The power the open fraction is raised to (<c>alchemy_density</c>, default one).</param>
public sealed record SsaoSettings(float Radius = 0.4f, float MaxRadiusUv = 0.08f, float Bias = 0.03f, int SamplePairs = 5, float Turns = 7f, float Intensity = 1f, float DetectionIntensity = 1f, float Contrast = 1f, IReadOnlyList<float>? Weights = null, float BlurDepthSharpness = 30f)
{
    public IReadOnlyList<float> BlurWeights { get; } = Weights ?? [1f, 0.85f, 0.6f, 0.35f];
}
