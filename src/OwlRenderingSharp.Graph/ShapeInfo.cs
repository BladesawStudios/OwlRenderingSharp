using System.Numerics;
using BfShaderSharp.Pack.Records;

namespace OwlRenderingSharp.Graph;

/// <summary>What the shape-level blocks are filled from: the shape and its model's skeleton, and where the model is placed.</summary>
public sealed record ShapeInfo(string Name, ShapeRecord Shape, SkeletonRecord Skeleton, Matrix4x4 World);
