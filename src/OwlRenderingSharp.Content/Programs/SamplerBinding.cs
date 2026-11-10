namespace OwlRenderingSharp.Content.Programs;

/// <summary>A sampler of a program tied to the shader's own name for it (<c>_a0</c>, <c>gsys_depth_buffer</c>), with the unit and type its GLSL declares.</summary>
/// <remarks>The unit is the binding qualifier in the source.</remarks>
public sealed record SamplerBinding(string GlslName, bool Vertex, int Unit, string Type, string ShaderName);
