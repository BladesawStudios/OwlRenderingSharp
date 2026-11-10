using System.Numerics;

namespace OwlRenderingSharp.Graph;

/// <summary>One slice of the view frustum and the light camera that renders its shadow map layer.</summary>
/// <remarks>
/// <see cref="TextureMatrix"/> takes a world position, as a row vector, to the shadow texture: x and y in zero to one across the layer and z the zero-to-one light depth to compare against.
/// <see cref="Split"/> is the view distance at which the slice ends.
/// </remarks>
public sealed record ShadowCascade(Camera Light, Matrix4x4 TextureMatrix, float Split);
