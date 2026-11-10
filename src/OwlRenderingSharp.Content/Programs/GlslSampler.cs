namespace OwlRenderingSharp.Content.Programs;

/// <summary>A sampler uniform as a decompiled program declares it: its GLSL name, binding qualifier and GLSL type.</summary>
/// <remarks>The type is the keyword, such as <c>sampler2D</c>, <c>samplerCube</c> or <c>sampler3D</c>.</remarks>
public sealed record GlslSampler(string Name, int Binding, string Type);
