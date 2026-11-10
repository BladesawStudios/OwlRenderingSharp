namespace OwlRenderingSharp.Gpu;

/// <summary>A sampler uniform of a linked program: its name in the shader, its kind and the texture unit its binding qualifier assigns.</summary>
/// <remarks>The decompiled programs name samplers by the slot the game bound them at and fix the unit in the source, so the unit is read back from the program.</remarks>
public sealed record GpuSamplerUniform(string Name, GpuSamplerKind Kind, int Unit);
