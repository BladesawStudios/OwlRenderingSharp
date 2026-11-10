using System.Numerics;
using BfShaderSharp.Pack.Records;
using OwlRenderingSharp.Gpu;

namespace OwlRenderingSharp.Graph;

/// <summary>Reads a material's render info into the <see cref="DrawState"/> its draws use.</summary>
/// <remarks>
/// The display face picks the culling: front draws front faces, back draws back faces, both draws both and none draws nothing. The depth test, depth write and depth function
/// come from <c>gsys_depth_test_*</c>. The render state mode sets the starting point: <c>translucent</c> blends source alpha over the target and stops writing depth, every
/// other mode (opaque, mask, custom) writes the colour as it is. A blend mode then decides: <c>color</c> blends with the operations, factors and constant colour of the
/// <c>gsys_color_blend_*</c> infos, over the translucent defaults where an info is missing; <c>logic</c> replaces blending with the logic op named by <c>gsys_logic_op</c>
/// (no OoT material uses it, so that name is the exporter's convention, not one read from the game); <c>none</c> writes the colour as it is. Alpha testing is a shader option
/// and a material block value, so it is chosen with the program and not set here. Words come from <see cref="RenderStateWords"/>.
/// </remarks>
public static class MaterialDrawStates
{
    private const string Translucent = "translucent";

    public static DrawState From(MaterialRecord material)
    {
        DrawState state = DrawState.Opaque;
        string? face = Text(material, "gsys_render_state_display_face");
        return state with
        {
            Cull = face switch { "back" => CullMode.Front, "both" => CullMode.None, "none" => CullMode.Both, _ => CullMode.Back },
            DepthTest = Text(material, "gsys_depth_test_enable") is not "false",
            DepthWrite = Text(material, "gsys_depth_test_write") is "true" || (Text(material, "gsys_depth_test_write") is null && !IsTranslucent(material)),
            Blend = Blend(material),
            DepthFunc = RenderStateWords.Compare(Text(material, "gsys_depth_test_func")),
            Raster = Raster(material),
        };
    }

    // Whether the draw blends decides where it is sorted, so the drawer asks the same question of a record.
    public static bool Blends(MaterialRecord material) => Text(material, "gsys_render_state_blend_mode") == "color" || (IsTranslucent(material) && Text(material, "gsys_render_state_blend_mode") is not ("none" or "logic"));

    // The draw order among materials of the same kind: lower first.
    public static int Priority(MaterialRecord material) => material.RenderInfos.FirstOrDefault(r => r.Name == "gsys_priority")?.Ints.FirstOrDefault() ?? 0;

    private static bool IsTranslucent(MaterialRecord material) => Text(material, "gsys_render_state_mode") == Translucent;

    private static RasterState Raster(MaterialRecord material) =>
        Text(material, "gsys_render_state_blend_mode") == "logic" ? RasterState.Normal with { Logic = RenderStateWords.Logic(Text(material, "gsys_logic_op")) } : RasterState.Normal;

    private static BlendState Blend(MaterialRecord material)
    {
        if (!Blends(material)) return BlendState.Off;
        float[] constant = material.RenderInfos.FirstOrDefault(r => r.Name == "gsys_color_blend_const_color")?.Floats ?? [];
        return new BlendState(
            true,
            RenderStateWords.Op(Text(material, "gsys_color_blend_rgb_op")),
            RenderStateWords.Factor(Text(material, "gsys_color_blend_rgb_src_func"), BlendFactor.SrcAlpha),
            RenderStateWords.Factor(Text(material, "gsys_color_blend_rgb_dst_func"), BlendFactor.OneMinusSrcAlpha),
            RenderStateWords.Op(Text(material, "gsys_color_blend_alpha_op")),
            RenderStateWords.Factor(Text(material, "gsys_color_blend_alpha_src_func"), BlendFactor.One),
            RenderStateWords.Factor(Text(material, "gsys_color_blend_alpha_dst_func"), BlendFactor.OneMinusSrcAlpha),
            constant.Length >= 4 ? new Vector4(constant[0], constant[1], constant[2], constant[3]) : Vector4.Zero);
    }

    private static string? Text(MaterialRecord material, string name) =>
        material.RenderInfos.FirstOrDefault(r => r.Name == name)?.Strings.FirstOrDefault();
}
