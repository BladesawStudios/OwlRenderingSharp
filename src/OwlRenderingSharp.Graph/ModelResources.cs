using BfShaderSharp.Pack.Records;

namespace OwlRenderingSharp.Graph;

/// <summary>A model ready to draw: its skeleton, its shapes and the materials they use.</summary>
/// <remarks>It owns the material resources and disposes them; geometry and textures stay with the shared caches.</remarks>
public sealed class ModelResources(string name, SkeletonRecord skeleton, IReadOnlyList<ShapeResources> shapes, IReadOnlyList<MaterialResources> materials) : IDisposable
{
    public string Name { get; } = name;

    public SkeletonRecord Skeleton { get; } = skeleton;

    public IReadOnlyList<ShapeResources> Shapes { get; } = shapes;

    public IReadOnlyList<MaterialResources> Materials { get; } = materials;

    public void Dispose()
    {
        foreach (MaterialResources material in Materials) material.Dispose();
    }
}
