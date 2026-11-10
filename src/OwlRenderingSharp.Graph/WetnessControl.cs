namespace OwlRenderingSharp.Graph;

/// <summary>Sets how wet a model looks, through the one material block field the game's wet-surface shading is known to read.</summary>
/// <remarks>
/// The game's model programs darken and saturate a non-metal surface's colour by <c>p_absorption_ratio</c> (material block byte 0x324; the colour is mixed toward albedo * (1 - 0.8 * ratio)
/// and scaled by one minus the metalness, so a wet cloth or skin is deeper and a wet metal is unchanged). That is the only wet-related field the character programs read: the
/// neighbours named <c>p_rain_wet_valid_ratio</c> (0x328), <c>p_local_rain_wet_ratio</c> (0x360) and <c>p_local_rain_wet_mask_ratio</c> (0x364) are not read as wetness (writing them
/// made skin matte), and no roughness or gloss term of these programs follows a wetness value. So this control gives the darkening of wet, not its sheen. The place palette's
/// <c>HumidityWetRatio</c> is the game's own level for a place. The write patches the blocks of the model's materials in place, so call it again with 0 to dry the model.
/// </remarks>
public static class WetnessControl
{
    public const int Absorption = 0x324, MinimumBlockBytes = Absorption + 4;

    public static float Clamp(float amount) => Math.Clamp(amount, 0f, 1f);

    public static void Apply(ModelResources model, float amount)
    {
        float wet = Clamp(amount);
        foreach (MaterialResources material in model.Materials)
            if (material.Block is { } block && block.ByteSize >= MinimumBlockBytes)
                block.Write<float>([wet], Absorption);
    }
}
