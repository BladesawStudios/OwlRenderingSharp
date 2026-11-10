namespace OwlRenderingSharp.Graph;

/// <summary>A texture and sampler a frame provides for a frame-level sampler, by the sampler's engine name.</summary>
public readonly record struct FrameTexture(uint Texture, uint Sampler);
