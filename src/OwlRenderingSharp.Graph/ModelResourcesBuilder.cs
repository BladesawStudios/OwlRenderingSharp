using BfShaderSharp.Errors;
using BfShaderSharp.Pack.Records;

namespace OwlRenderingSharp.Graph;

/// <summary>Builds the <see cref="ModelResources"/> of a model the content set holds.</summary>
/// <remarks>
/// A model the packs lack, or a shape that names a material the model does not have, throws <see cref="BfShaderFormatException"/> naming the model.
/// A shape's geometry is built for the attribute locations of the shading model of the material it uses.
/// </remarks>
public sealed class ModelResourcesBuilder(RenderServices services)
{
    public ModelResources Build(string name)
    {
        ModelRecord model = services.Content.FindModel(name) ?? throw new BfShaderFormatException($"No pack has the model {name}");
        MaterialResourcesBuilder materials = new(services);
        List<MaterialResources> built = [];
        try
        {
            foreach (MaterialRef material in model.Materials) built.Add(materials.Build($"{name}/{material.Name}", services.Content.Material(material.Material)));
            List<ShapeResources> shapes = [];
            foreach (ShapeRef shape in model.Shapes)
            {
                if (shape.MaterialIndex >= built.Count) throw new BfShaderFormatException($"Shape {shape.Name} of {name} uses material {shape.MaterialIndex}");
                ProgramEntry program = built[shape.MaterialIndex].Program;
                shapes.Add(new ShapeResources(
                    shape.Name,
                    services.Content.Shape(shape.Shape),
                    services.Resources.Geometry($"{name}/{shape.Name}", shape.Shape, program.LayoutId, program.Attributes),
                    shape.MaterialIndex,
                    shape.BoneIndex));
            }

            return new ModelResources(name, services.Content.Skeleton(model.Skeleton), shapes, built);
        }
        catch
        {
            foreach (MaterialResources material in built) material.Dispose();
            throw;
        }
    }
}
