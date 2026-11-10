using BfShaderSharp.Pack;
using BfShaderSharp.Pack.Records;
using OwlRenderingSharp.Content.Programs;
using OwlRenderingSharp.Gpu;

namespace OwlRenderingSharp.Graph;

/// <summary>Builds the <see cref="MaterialResources"/> of a material record and the per-pass programs it asks for later.</summary>
/// <remarks>
/// The program of the material itself is the one the pack recorded; a pass picks its own through the <see cref="PassProgramSelector"/>. Each sampler of a program is
/// resolved through the material's sampler assigns to a sampler record and from it to a texture; a sampler the material does not assign, or whose texture no pack has,
/// gets the neutral texture of its kind (coloured by <see cref="FrameSamplerDefaults"/> for the frame-level ones) so the read stays defined. The material block starts as
/// the bytes the pack prepared, padded with zeros to the size the program declares.
/// </remarks>
public sealed class MaterialResourcesBuilder(RenderServices services)
{
    public const string MaterialBlock = "gsys_material";
    private const string DefaultValue = "<Default Value>";

    public MaterialResources Build(string name, MaterialRecord material)
    {
        ProgramEntry program = services.Programs.Get(material.ShaderArchive, material.ShadingModel, material.Program);
        return new MaterialResources(name, material, program, MaterialDrawStates.From(material), Block(name, material, program), this);
    }

    // The program a pass runs this material with for a shape of the given skin weight count, or null when the shading model has none.
    public MaterialPass? CreatePass(string name, MaterialRecord material, RenderPass pass, int skinCount)
    {
        int index = services.Selector.Select(material, pass, skinCount);
        if (index < 0) return null;
        ProgramEntry program = services.Programs.Get(material.ShaderArchive, material.ShadingModel, index);
        return new MaterialPass(program, Units(name, material, program));
    }

    private GpuBuffer? Block(string name, MaterialRecord material, ProgramEntry program)
    {
        if (!program.BlockSizes.TryGetValue(MaterialBlock, out int size)) return null;
        GpuBuffer buffer = new(services.Device, $"{name} material block", size);
        if (material.Block == default) return buffer;
        byte[] bytes = services.Content.Bytes(material.Block);
        buffer.Write<byte>(bytes.AsSpan(0, Math.Min(bytes.Length, size)));
        return buffer;
    }

    private List<TextureUnit> Units(string name, MaterialRecord material, ProgramEntry program)
    {
        IReadOnlyDictionary<string, GpuSamplerKind> kinds = program.Program.Samplers().ToDictionary(s => s.Name, s => s.Kind);
        List<TextureUnit> units = [];
        foreach (SamplerBinding binding in program.Bindings.Samplers)
        {
            GpuSamplerKind kind = kinds.GetValueOrDefault(binding.GlslName, GpuSamplerKind.Texture2D);
            SamplerRecord? sampler = Assigned(material, binding.ShaderName);
            TextureRecord? record = sampler is null ? null : services.Content.FindTexture(sampler.Texture);
            if (sampler is not null && record is not null && kind == GpuSamplerKind.Texture2D)
            {
                GpuTexture texture = services.Resources.Texture(sampler.Texture, record);
                units.Add(new TextureUnit(binding.Unit, texture.Handle, services.Samplers.Get(MaterialSamplerStates.From(sampler)).Handle));
            }
            else
            {
                units.Add(new TextureUnit(binding.Unit, services.Neutral.Handle(kind, FrameSamplerDefaults.For(binding.ShaderName)), services.Samplers.Get(SamplerState.LinearClamp).Handle));
            }
        }

        return units;
    }

    private static SamplerRecord? Assigned(MaterialRecord material, string shaderName)
    {
        string? assigned = material.SamplerAssigns.FirstOrDefault(a => a.Key == shaderName)?.Value;
        if (assigned is null or DefaultValue) return null;
        return material.Samplers.FirstOrDefault(s => s.Name == assigned);
    }
}
