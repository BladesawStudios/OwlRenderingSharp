namespace OwlRenderingSharp.Blocks;

/// <summary>One named, typed field of a uniform block at a byte offset, with the evidence for what it is.</summary>
/// <remarks>
/// The evidence is a line of text saying how the field's meaning is known: the shader that reads it, the engine function that writes it,
/// or that it is only inferred. A field with no evidence is not allowed in a layout file.
/// </remarks>
public sealed record BlockField(string Name, int Offset, BlockFieldType Type, string Evidence)
{
    public int ByteSize => Type switch
    {
        BlockFieldType.Float or BlockFieldType.Int or BlockFieldType.UInt => 4,
        BlockFieldType.Vector2 => 8,
        BlockFieldType.Vector3 => 12,
        BlockFieldType.Vector4 => 16,
        BlockFieldType.Matrix3x4 => 48,
        BlockFieldType.Matrix4x4 => 64,
        _ => throw new ArgumentOutOfRangeException(nameof(Type)),
    };
}
