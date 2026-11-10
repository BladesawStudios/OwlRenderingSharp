namespace OwlRenderingSharp.Graph;

/// <summary>The passes a model is drawn in, in order.</summary>
/// <remarks>
/// <see cref="Forward"/> is the depth pre-pass followed by the colour pass: the first fills the depth buffer with the opaque shapes and no colour, so the second shades only
/// the surfaces that remain. <see cref="Shadow"/> is the depth shadow pass a <see cref="ShadowMap"/> runs once per cascade. Cube map passes draw into other targets and are planned by the frame, not here.
/// </remarks>
public sealed record PassPlan(IReadOnlyList<RenderPass> Passes)
{
    public static PassPlan Forward { get; } = new([RenderPass.ZPrepass, RenderPass.Material]);

    public static PassPlan ColorOnly { get; } = new([RenderPass.Material]);

    public static PassPlan Shadow { get; } = new([RenderPass.DepthShadow]);
}
