using BfShaderSharp.Pack.Records;
using OwlRenderingSharp.Gpu;

namespace OwlRenderingSharp.Graph;

/// <summary>Reads a material's render info into the <see cref="DrawState"/> its draws use.</summary>
/// <remarks>
/// The display face picks the culling: front draws front faces, back draws back faces, both draws both and none draws nothing. The depth test, depth write and depth function
/// come from <c>gsys_depth_test_*</c>. A render info the material lacks keeps the opaque default.
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

    private static string? Text(MaterialRecord material, string name) =>
        material.RenderInfos.FirstOrDefault(r => r.Name == name)?.Strings.FirstOrDefault();
}
