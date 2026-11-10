using System.Buffers.Binary;
using System.Numerics;

namespace OwlRenderingSharp.Blocks;

/// <summary>Fills the bytes of a uniform block by field name, checked against its <see cref="BlockLayout"/>.</summary>
/// <remarks>
/// Setting a field that the layout does not have, or with a value of another type, throws. The bytes start at zero, or as a copy of a given block
/// (a material's prebuilt block that a renderer then changes a few fields of). <see cref="Bytes"/> is the live buffer, to upload when filled.
/// </remarks>
public sealed class BlockWriter
{
    private readonly byte[] _bytes;

    public BlockWriter(BlockLayout layout)
    {
        Layout = layout;
        _bytes = new byte[layout.Size];
    }

    public BlockWriter(BlockLayout layout, ReadOnlySpan<byte> start) : this(layout)
    {
        if (start.Length != layout.Size) throw new ArgumentException($"{layout.Name} is {layout.Size} bytes, not {start.Length}");
        start.CopyTo(_bytes);
    }

    public BlockLayout Layout { get; }

    public byte[] Bytes => _bytes;

    public BlockWriter Set(string name, float value)
    {
        Write(name, BlockFieldType.Float, [value]);
        return this;
    }

    public BlockWriter Set(string name, int value)
    {
        BinaryPrimitives.WriteInt32LittleEndian(_bytes.AsSpan(Field(name, BlockFieldType.Int).Offset), value);
        return this;
    }

    public BlockWriter Set(string name, uint value)
    {
        BinaryPrimitives.WriteUInt32LittleEndian(_bytes.AsSpan(Field(name, BlockFieldType.UInt).Offset), value);
        return this;
    }

    public BlockWriter Set(string name, Vector2 value)
    {
        Write(name, BlockFieldType.Vector2, [value.X, value.Y]);
        return this;
    }

    public BlockWriter Set(string name, Vector3 value)
    {
        Write(name, BlockFieldType.Vector3, [value.X, value.Y, value.Z]);
        return this;
    }

    public BlockWriter Set(string name, Vector4 value)
    {
        Write(name, BlockFieldType.Vector4, [value.X, value.Y, value.Z, value.W]);
        return this;
    }

    // A matrix is written as the rows of its transpose: three for a 3x4 field, four for a 4x4 field.
    public BlockWriter Set(string name, Matrix4x4 value)
    {
        BlockField field = Layout.Field(name);
        if (field.Type is not (BlockFieldType.Matrix3x4 or BlockFieldType.Matrix4x4)) throw new ArgumentException($"{Layout.Name}.{name} is a {field.Type}, not a matrix");
        Matrix4x4 t = Matrix4x4.Transpose(value);
        float[] rows = [t.M11, t.M12, t.M13, t.M14, t.M21, t.M22, t.M23, t.M24, t.M31, t.M32, t.M33, t.M34, t.M41, t.M42, t.M43, t.M44];
        Write(name, field.Type, rows.AsSpan(0, field.ByteSize / 4));
        return this;
    }

    public float GetFloat(string name) => BinaryPrimitives.ReadSingleLittleEndian(_bytes.AsSpan(Field(name, BlockFieldType.Float).Offset));

    public Vector4 GetVector4(string name)
    {
        int at = Field(name, BlockFieldType.Vector4).Offset;
        return new Vector4(Read(at), Read(at + 4), Read(at + 8), Read(at + 12));
    }

    private float Read(int at) => BinaryPrimitives.ReadSingleLittleEndian(_bytes.AsSpan(at));

    private BlockField Field(string name, BlockFieldType expected)
    {
        BlockField field = Layout.Field(name);
        if (field.Type != expected) throw new ArgumentException($"{Layout.Name}.{name} is a {field.Type}, not a {expected}");
        return field;
    }

    private void Write(string name, BlockFieldType type, ReadOnlySpan<float> values)
    {
        int at = Layout.Field(name).Offset;
        if (Layout.Field(name).Type != type) throw new ArgumentException($"{Layout.Name}.{name} is a {Layout.Field(name).Type}, not a {type}");
        for (int i = 0; i < values.Length; i++) BinaryPrimitives.WriteSingleLittleEndian(_bytes.AsSpan(at + i * 4), values[i]);
    }
}
