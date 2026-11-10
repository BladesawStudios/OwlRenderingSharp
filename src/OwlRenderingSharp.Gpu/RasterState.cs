namespace OwlRenderingSharp.Gpu;

/// <summary>The rasteriser settings of a draw that are not culling or depth: polygon mode, winding, depth bias, alpha to coverage and the logic op.</summary>
/// <remarks>
/// The default value is the ordinary one: filled, counter-clockwise front faces, no depth bias, no alpha to coverage and no logic op. A logic op replaces blending
/// while it is on, as it does on the console.
/// </remarks>
public readonly record struct RasterState(PolygonMode Polygon, FrontFace Front, float DepthBiasSlope, float DepthBiasUnits, bool AlphaToCoverage, LogicOp Logic)
{
    public static RasterState Normal => default;
}
