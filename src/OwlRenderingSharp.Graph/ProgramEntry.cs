using OwlRenderingSharp.Content.Programs;
using OwlRenderingSharp.Gpu;

namespace OwlRenderingSharp.Graph;

/// <summary>A linked program with what it binds, in the engine's terms.</summary>
/// <remarks>
/// The program's uniform blocks are already moved to the renderer's binding points. Attributes are the shading model's vertex attribute names with their locations, and
/// BlockSizes the byte size of each of its uniform blocks by engine name, which is what a buffer for that block must hold. LayoutId names the attribute layout, since
/// Index is the program number in its shading model. Geometry built for one shading model's locations cannot be shared with another's.
/// </remarks>
public sealed record ProgramEntry(
    GpuProgram Program,
    ProgramBindings Bindings,
    string LayoutId,
    IReadOnlyDictionary<string, int> Attributes,
    IReadOnlyDictionary<string, int> BlockSizes,
    int Index);
