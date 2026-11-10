using OwlRenderingSharp.Content.Programs;

namespace OwlRenderingSharp.Graph;

/// <summary>The alpha test a material draws with: the compare function (a <see cref="GlslAlphaTest"/> code, zero for off) and the reference the written alpha is compared with.</summary>
public readonly record struct AlphaTest(uint Function, float Reference);
