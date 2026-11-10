using BfShaderSharp.Pack.Records;
using OwlRenderingSharp.Gpu;

namespace OwlRenderingSharp.Graph;

/// <summary>A shape of a model ready to draw: its shared geometry, the material it uses and its record.</summary>
public sealed record ShapeResources(string Name, ShapeRecord Record, GpuGeometry Geometry, int MaterialIndex, int BoneIndex);
