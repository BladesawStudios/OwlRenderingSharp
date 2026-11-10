namespace OwlRenderingSharp.Gpu;

/// <summary>What a source or destination colour is multiplied by before the blend operation combines them.</summary>
/// <remarks>
/// The list is every factor the GX2 blend function and the NVN blend state define. The Src1 factors read the second output of a dual-source fragment shader.
/// </remarks>
public enum BlendFactor
{
    Zero,
    One,
    SrcColor,
    OneMinusSrcColor,
    DstColor,
    OneMinusDstColor,
    SrcAlpha,
    OneMinusSrcAlpha,
    DstAlpha,
    OneMinusDstAlpha,
    ConstantColor,
    OneMinusConstantColor,
    ConstantAlpha,
    OneMinusConstantAlpha,
    SrcAlphaSaturate,
    Src1Color,
    OneMinusSrc1Color,
    Src1Alpha,
    OneMinusSrc1Alpha,
}
