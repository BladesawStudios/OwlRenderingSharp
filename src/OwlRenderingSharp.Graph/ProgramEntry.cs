using OwlRenderingSharp.Content.Programs;
using OwlRenderingSharp.Gpu;

namespace OwlRenderingSharp.Graph;

/// <summary>A linked program with what it binds, in the engine's terms.</summary>
/// <remarks>The program's uniform blocks are already moved to the renderer's binding points.</remarks>
public sealed record ProgramEntry(GpuProgram Program, ProgramBindings Bindings);
