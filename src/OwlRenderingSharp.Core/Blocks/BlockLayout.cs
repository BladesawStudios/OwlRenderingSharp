namespace OwlRenderingSharp.Blocks;

/// <summary>The size of a uniform block and its named fields.</summary>
/// <remarks>
/// A layout is validated when it is built: the size is a positive multiple of 16 (std140), every field fits inside the block, names are unique,
/// fields are aligned to their type and no two overlap. A block need not name every byte; the rest stays zero.
/// </remarks>
public sealed class BlockLayout
{
    private readonly Dictionary<string, BlockField> _byName;

    public BlockLayout(string name, int size, IReadOnlyList<BlockField> fields)
    {
        if (size <= 0 || size % 16 != 0) throw new ArgumentException($"{name}: a std140 block is a positive multiple of 16 bytes, not {size}");
        Name = name;
        Size = size;
        Fields = [.. fields.OrderBy(f => f.Offset)];
        _byName = new Dictionary<string, BlockField>(StringComparer.Ordinal);
        int end = 0;
        foreach (BlockField field in Fields)
        {
            if (!_byName.TryAdd(field.Name, field)) throw new ArgumentException($"{name}: the field {field.Name} is defined twice");
            if (field.Offset < 0 || field.Offset + field.ByteSize > size) throw new ArgumentException($"{name}: the field {field.Name} does not fit the block");
            if (field.Offset % Alignment(field.Type) != 0) throw new ArgumentException($"{name}: the field {field.Name} is not aligned");
            if (field.Offset < end) throw new ArgumentException($"{name}: the field {field.Name} overlaps the one before it");
            if (string.IsNullOrWhiteSpace(field.Evidence)) throw new ArgumentException($"{name}: the field {field.Name} has no evidence");
            end = field.Offset + field.ByteSize;
        }
    }

    public string Name { get; }

    public int Size { get; }

    public IReadOnlyList<BlockField> Fields { get; }

    public BlockField Field(string name) =>
        _byName.TryGetValue(name, out BlockField? field) ? field : throw new KeyNotFoundException($"{Name} has no field {name}");

    private static int Alignment(BlockFieldType type) => type switch
    {
        BlockFieldType.Float or BlockFieldType.Int or BlockFieldType.UInt => 4,
        BlockFieldType.Vector2 => 8,
        _ => 16,
    };
}
