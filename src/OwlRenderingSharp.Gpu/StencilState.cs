namespace OwlRenderingSharp.Gpu;

/// <summary>The stencil test of a draw, with its own settings for front and back faces.</summary>
public readonly record struct StencilState(bool Enabled, StencilFace Front, StencilFace Back)
{
    public static StencilState Off { get; } = new(false, StencilFace.Passthrough, StencilFace.Passthrough);
}
