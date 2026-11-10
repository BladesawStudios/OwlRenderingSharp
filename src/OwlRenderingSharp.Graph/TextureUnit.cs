namespace OwlRenderingSharp.Graph;

/// <summary>One texture unit a draw sets: the GL texture and sampler objects to bind to it, and the engine name of the sampler for a frame to replace.</summary>
public readonly record struct TextureUnit(int Unit, uint Texture, uint Sampler, string ShaderName = "");
