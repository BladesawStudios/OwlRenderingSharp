namespace OwlRenderingSharp.Gpu;

/// <summary>The comparison a depth test makes between a fragment's depth and the stored depth.</summary>
/// <remarks>A fragment passes when the comparison of its depth to the stored depth is true.</remarks>
public enum DepthFunc
{
    Never,
    Less,
    Equal,
    LessOrEqual,
    Greater,
    NotEqual,
    GreaterOrEqual,
    Always,
}
