using OwlRenderingSharp.Gpu;

namespace OwlRenderingSharp.Graph;

/// <summary>The colour a frame-level sampler reads while the renderer has no real buffer for it.</summary>
/// <remarks>
/// The shading programs sample buffers the frame produces (the depth buffer, the geometry buffer, the shadow pre-pass) and projected textures by their engine names. With
/// no such buffer, each reads one texel chosen so the read means "nothing there": no shadow, the far plane, no projection, and a dim sky for the reflection cube (a white one blows out every glossy surface). A name not listed is white.
/// A material sampler a material leaves on its default value reads the engine's default for its role: the metalness maps (<c>_m0</c>, <c>_m1</c>) are black, because a white one makes the whole surface
/// metal and, with nothing to reflect, black (the leather hat band), and the normal maps (<c>_n0</c>, <c>_n1</c>) are flat. Colours are packed red in the low byte to alpha in the high byte.
/// </remarks>
public static class FrameSamplerDefaults
{
    private const uint White = 0xFFFFFFFF, Black = 0xFF000000, Clear = 0x00000000, FlatNormal = 0x00FF8080, FlatNormalMap = 0xFFFF8080, DimSky = 0xFF101010;

    private static readonly Dictionary<string, uint> Colors = new()
    {
        ["gsys_depth_buffer"] = White,
        ["gsys_gbuffer_albedo"] = Black,
        ["gsys_gbuffer_material_id"] = Clear,
        ["gsys_normal_modify"] = FlatNormal,
        ["gsys_shadow_prepass"] = White,
        ["gsys_depth_shadow_quarter"] = White,
        ["gsys_projection0"] = Clear,
        ["gsys_projection1"] = Clear,
        ["gsys_cube_map"] = DimSky,
        ["_m0"] = Black,
        ["_m1"] = Black,
        ["_n0"] = FlatNormalMap,
        ["_n1"] = FlatNormalMap,
    };

    public static uint For(string shaderName) => Colors.GetValueOrDefault(shaderName, White);

    // A volume texture the frame does not provide holds light or reflection probes, and an empty one is dark; the other kinds keep the name's own default.
    public static uint For(string shaderName, GpuSamplerKind kind) => kind == GpuSamplerKind.Texture3D && !Colors.ContainsKey(shaderName) ? Black : For(shaderName);
}
