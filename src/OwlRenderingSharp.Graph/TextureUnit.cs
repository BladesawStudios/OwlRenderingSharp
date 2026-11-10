namespace OwlRenderingSharp.Graph;

/// <summary>One texture unit a draw sets: the GL texture and sampler objects to bind to it.</summary>
public readonly record struct TextureUnit(int Unit, uint Texture, uint Sampler);
