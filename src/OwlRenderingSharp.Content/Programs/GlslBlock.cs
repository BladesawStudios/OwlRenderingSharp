namespace OwlRenderingSharp.Content.Programs;

/// <summary>A uniform or storage block as a decompiled program declares it: its GLSL name, binding qualifier and whether it is a storage block.</summary>
/// <remarks>The name is the block name after <c>uniform</c> or <c>buffer</c>, such as <c>_vp_c6</c>, <c>_fp_s0</c> or <c>_support_buffer</c>.</remarks>
public sealed record GlslBlock(string Name, int Binding, bool IsStorage);
