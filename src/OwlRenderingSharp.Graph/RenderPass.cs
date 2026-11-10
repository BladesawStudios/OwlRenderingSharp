namespace OwlRenderingSharp.Graph;

/// <summary>The kind of draw a shading model's program was compiled for.</summary>
/// <remarks>
/// A shading model holds one program per option combination, and the <c>gsys_assign_type</c> option says which pass the program belongs to: the colour pass of a material,
/// the depth-only pre-pass, the shadow depth pass, the depth pre-pass of translucent surfaces, the cube map pass or the debug visualisation.
/// </remarks>
public enum RenderPass
{
    Material,
    ZPrepass,
    DepthShadow,
    XluZPrepass,
    Cubemap,
    Visualize,
}

public static class RenderPasses
{
    // The choice of the gsys_assign_type option a pass selects.
    public static string AssignType(this RenderPass pass) => pass switch
    {
        RenderPass.ZPrepass => "gsys_assign_zonly",
        RenderPass.DepthShadow => "gsys_assign_depthshadow",
        RenderPass.XluZPrepass => "gsys_assign_xlu_zprepass",
        RenderPass.Cubemap => "gsys_assign_cubemap",
        RenderPass.Visualize => "gsys_assign_visualize",
        _ => "gsys_assign_material",
    };

    // Whether a pass only fills the depth buffer, so its colour writes are off.
    public static bool DepthOnly(this RenderPass pass) => pass is RenderPass.ZPrepass or RenderPass.DepthShadow or RenderPass.XluZPrepass;
}
