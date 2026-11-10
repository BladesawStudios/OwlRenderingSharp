namespace OwlRenderingSharp.Blocks;

/// <summary>The types a field of a uniform block can have.</summary>
/// <remarks>
/// Matrices are stored the way the games' shaders read them: the transpose of a System.Numerics matrix, as rows of four floats, three rows
/// for <see cref="Matrix3x4"/> and four for <see cref="Matrix4x4"/>.
/// </remarks>
public enum BlockFieldType
{
    Float,
    Int,
    UInt,
    Vector2,
    Vector3,
    Vector4,
    Matrix3x4,
    Matrix4x4,
}
