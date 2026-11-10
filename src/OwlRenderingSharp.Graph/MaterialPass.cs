namespace OwlRenderingSharp.Graph;

/// <summary>The program a material runs in one pass and the texture units that program reads.</summary>
/// <remarks>Texture units depend on the program, since each program numbers its samplers its own way, so a pass carries its own list.</remarks>
public sealed record MaterialPass(ProgramEntry Program, IReadOnlyList<TextureUnit> Units);
