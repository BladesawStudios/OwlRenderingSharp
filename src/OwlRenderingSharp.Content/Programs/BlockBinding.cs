namespace OwlRenderingSharp.Content.Programs;

/// <summary>A uniform block of a program tied to the engine block it holds: the stage's GLSL block and the <c>gsys_*</c> name.</summary>
/// <remarks>Vertex says which stage's declaration it is; a block both stages read appears once for each.</remarks>
public sealed record BlockBinding(string GlslName, bool Vertex, string GsysName);
