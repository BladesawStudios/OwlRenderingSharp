namespace OwlRenderingSharp.Graph;

/// <summary>Sets how wet a model looks: the material half of the wetness control.</summary>
/// <remarks>
/// The game's model programs take wetness from several places, found by comparing sibling programs that differ only in their wet options and by sweeping slots on renders.
/// <c>p_absorption_ratio</c> (material block byte 0x324) darkens and saturates a non-metal colour: the colour becomes albedo * (1 - 0.8 * ratio squared), scaled by one minus the
/// metalness. This is the only term measured to change the characters and items rendered so far. <c>p_rain_wet_valid_ratio</c> (0x328) times the third channel of the packed mask texture
/// feeds a normal flatten and specular lift scaled by the environment block's slot 55 .z (<c>OotBlockProfile.Wetness</c>), but renders of Link and Zelda were pixel-identical with it on, so
/// their programs do not appear to use it. The local rain wet fields (0x360, 0x364) are read by 920 programs, none of which the characters or items here resolve to.
/// Eye materials are left out: the valid ratio lifts the floor under their light term and removes the catchlights.
/// The place palette's <c>HumidityWetRatio</c> is the game's own level for a place. The writes patch the blocks of the model's materials in place; call it with 0 to dry the model.
/// </remarks>
public static class WetnessControl
{
    public const int Absorption = 0x324, ValidRatio = 0x328, MinimumBlockBytes = ValidRatio + 4;

    public static float Clamp(float amount) => Math.Clamp(amount, 0f, 1f);

    public static bool Wettable(string materialName) => !materialName.Contains("Eye", StringComparison.OrdinalIgnoreCase);

    public static void Apply(ModelResources model, float amount)
    {
        float wet = Clamp(amount);
        foreach (MaterialResources material in model.Materials)
        {
            if (material.Block is not { } block || block.ByteSize < MinimumBlockBytes) continue;
            block.Write<float>([wet], Absorption);
            float valid = Wettable(material.Name) ? wet : 0f;
            block.Write<float>([valid], ValidRatio);
        }
    }
}
