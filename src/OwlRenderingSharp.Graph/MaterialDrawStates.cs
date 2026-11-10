using System.Numerics;
using BfShaderSharp.Pack.Records;
using OwlRenderingSharp.Gpu;

namespace OwlRenderingSharp.Graph;

/// <summary>Reads a material's render info into the <see cref="DrawState"/> its draws use.</summary>
/// <remarks>
/// The display face picks the culling: front draws front faces, back draws back faces, both draws both and none draws nothing. The depth test, depth write and depth function
/// come from <c>gsys_depth_test_*</c>. A blend mode of <c>color</c> turns blending on with the operations, factors and constant colour of the <c>gsys_color_blend_*</c> infos; any
/// other mode writes the colour as it is. A render info the material lacks keeps the opaque default.
/// </remarks>
public static class MaterialDrawStates
{
    public static DrawState From(MaterialRecord material)
    {
        DrawState state = DrawState.Opaque;
        string? face = Text(material, "gsys_render_state_display_face");
        string? func = Text(material, "gsys_depth_test_func");
        return state with
        {
            Cull = face switch { "back" => CullMode.Front, "both" => CullMode.None, "none" => CullMode.Both, _ => CullMode.Back },
            DepthTest = Text(material, "gsys_depth_test_enable") is not "false",
            DepthWrite = Text(material, "gsys_depth_test_write") is not "false",
            Blend = Blend(material),
            DepthFunc = func switch
            {
                "never" => DepthFunc.Never,
                "less" => DepthFunc.Less,
                "equal" => DepthFunc.Equal,
                "greater" => DepthFunc.Greater,
                "notequal" => DepthFunc.NotEqual,
                "gequal" => DepthFunc.GreaterOrEqual,
                "always" => DepthFunc.Always,
                _ => DepthFunc.LessOrEqual,
            },
        };
    }

    // Whether the draw blends decides where it is sorted, so the drawer asks the same question of a record.
    public static bool Blends(MaterialRecord material) => Text(material, "gsys_render_state_blend_mode") == "color";

    // The draw order among materials of the same kind: lower first.
    public static int Priority(MaterialRecord material) => material.RenderInfos.FirstOrDefault(r => r.Name == "gsys_priority")?.Ints.FirstOrDefault() ?? 0;

    private static BlendState Blend(MaterialRecord material)
    {
        if (!Blends(material)) return BlendState.Off;
        float[] constant = material.RenderInfos.FirstOrDefault(r => r.Name == "gsys_color_blend_const_color")?.Floats ?? [];
        return new BlendState(
            true,
            Op(Text(material, "gsys_color_blend_rgb_op")),
            Factor(Text(material, "gsys_color_blend_rgb_src_func"), BlendFactor.One),
            Factor(Text(material, "gsys_color_blend_rgb_dst_func"), BlendFactor.Zero),
            Op(Text(material, "gsys_color_blend_alpha_op")),
            Factor(Text(material, "gsys_color_blend_alpha_src_func"), BlendFactor.One),
            Factor(Text(material, "gsys_color_blend_alpha_dst_func"), BlendFactor.Zero),
            constant.Length >= 4 ? new Vector4(constant[0], constant[1], constant[2], constant[3]) : Vector4.Zero);
    }

    private static BlendOp Op(string? word) => word switch
    {
        "sub" or "subtract" => BlendOp.Subtract,
        "rev_sub" or "reverse_sub" or "reverse_subtract" => BlendOp.ReverseSubtract,
        "min" => BlendOp.Min,
        "max" => BlendOp.Max,
        _ => BlendOp.Add,
    };

    private static BlendFactor Factor(string? word, BlendFactor fallback) => word switch
    {
        "zero" => BlendFactor.Zero,
        "one" => BlendFactor.One,
        "src_color" => BlendFactor.SrcColor,
        "one_minus_src_color" => BlendFactor.OneMinusSrcColor,
        "dst_color" => BlendFactor.DstColor,
        "one_minus_dst_color" => BlendFactor.OneMinusDstColor,
        "src_alpha" => BlendFactor.SrcAlpha,
        "one_minus_src_alpha" => BlendFactor.OneMinusSrcAlpha,
        "dst_alpha" => BlendFactor.DstAlpha,
        "one_minus_dst_alpha" => BlendFactor.OneMinusDstAlpha,
        "const_color" or "constant_color" => BlendFactor.ConstantColor,
        "one_minus_const_color" or "one_minus_constant_color" => BlendFactor.OneMinusConstantColor,
        "const_alpha" or "constant_alpha" => BlendFactor.ConstantAlpha,
        "one_minus_const_alpha" or "one_minus_constant_alpha" => BlendFactor.OneMinusConstantAlpha,
        _ => fallback,
    };

    private static string? Text(MaterialRecord material, string name) =>
        material.RenderInfos.FirstOrDefault(r => r.Name == name)?.Strings.FirstOrDefault();
}
