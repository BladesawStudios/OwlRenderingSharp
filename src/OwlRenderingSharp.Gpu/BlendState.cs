using System.Numerics;

namespace OwlRenderingSharp.Gpu;

/// <summary>How a draw's colour is combined with what is already in the target.</summary>
/// <remarks>Colour and alpha have their own operation and factors. <see cref="Off"/> writes the colour as it is.</remarks>
public readonly record struct BlendState(bool Enabled, BlendOp RgbOp, BlendFactor RgbSource, BlendFactor RgbDestination, BlendOp AlphaOp, BlendFactor AlphaSource, BlendFactor AlphaDestination, Vector4 Constant)
{
    public static BlendState Off { get; } = new(false, BlendOp.Add, BlendFactor.One, BlendFactor.Zero, BlendOp.Add, BlendFactor.One, BlendFactor.Zero, Vector4.Zero);
}
