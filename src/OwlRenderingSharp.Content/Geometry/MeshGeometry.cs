namespace OwlRenderingSharp.Content.Geometry;

/// <summary>One level of detail of a shape: its triangle indices.</summary>
/// <remarks>Indices are 16-bit or 32-bit little-endian in the order stored; they are relative to <see cref="FirstVertex"/>.</remarks>
public sealed record MeshGeometry(int IndexCount, bool Wide, byte[] Indices, int FirstVertex);
