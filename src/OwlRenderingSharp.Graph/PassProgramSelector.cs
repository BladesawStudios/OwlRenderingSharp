using BfShaderSharp.Lookup;
using BfShaderSharp.Pack.Records;
using OwlRenderingSharp.Content;

namespace OwlRenderingSharp.Graph;

/// <summary>Picks the program of a material's shading model for a pass and a shape.</summary>
/// <remarks>
/// The choices are the material's own, then the draw-time ones: the pass as <c>gsys_assign_type</c>, the shape's skin weight count as <c>gsys_weight</c> and the renderer's
/// <see cref="PassOptions"/>. When no program was compiled with every choice the nearest one is taken, weighing the pass first, then the skin count, then the renderer's
/// options, so a pass never silently becomes another. The engine turns the shadow pre-pass option on for materials that receive the pre-pass shadow whatever the material file says, so that option is weighed least: without that a metal material whose own program has it on was drawn with a program that has metalness and occlusion off. The result is -1 when the shader archive lacks the shading model. The lookups are built once per shading model and kept.
/// </remarks>
public sealed class PassProgramSelector(ContentSet content, PassOptions options)
{
    private const string AssignType = "gsys_assign_type", Weight = "gsys_weight", ShadowPrePass = "ns_enable_shadow_pre_pass";
    private readonly Dictionary<(string, string), ProgramLookup?> _lookups = [];

    public int Select(MaterialRecord material, RenderPass pass, int skinCount)
    {
        ProgramLookup? lookup = Lookup(material.ShaderArchive, material.ShadingModel);
        if (lookup is null) return -1;
        Dictionary<string, string> choices = material.Choices.ToDictionary(c => c.Key, c => c.Value);
        choices[AssignType] = pass.AssignType();
        choices[Weight] = skinCount.ToString();
        foreach (KeyValuePair<string, string> over in options.Overrides) choices[over.Key] = over.Value;
        int program = lookup.Find(choices);
        return program >= 0 ? program : lookup.FindNearest(choices, Weigh);
    }

    private int Weigh(string name) => name switch
    {
        AssignType => 10000,
        Weight => 1000,
        ShadowPrePass => 1,
        _ when options.Overrides.ContainsKey(name) => 100,
        _ when name.StartsWith("o_", StringComparison.Ordinal) => 1,
        _ => 16,
    };

    private ProgramLookup? Lookup(string archive, string shadingModel)
    {
        if (!_lookups.TryGetValue((archive, shadingModel), out ProgramLookup? lookup))
            _lookups[(archive, shadingModel)] = lookup = content.Shaders(archive).Model(shadingModel)?.CreateLookup();
        return lookup;
    }
}
