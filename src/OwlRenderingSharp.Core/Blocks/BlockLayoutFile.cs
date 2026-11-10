using System.Text.Json;
using System.Text.Json.Serialization;

namespace OwlRenderingSharp.Blocks;

/// <summary>Reads a <see cref="BlockLayout"/> from a layout file: JSON with the block's name and size and a list of fields.</summary>
/// <remarks>
/// A field is <c>{"name": ..., "offset": ..., "type": ..., "evidence": ...}</c> with the type named as in <see cref="BlockFieldType"/>
/// (case does not matter). A malformed file or an invalid layout throws an <see cref="InvalidDataException"/> naming the file's block.
/// </remarks>
public static class BlockLayoutFile
{
    private static readonly JsonSerializerOptions Options = new() { PropertyNameCaseInsensitive = true, Converters = { new JsonStringEnumConverter(allowIntegerValues: false) } };

    public static BlockLayout Parse(string json)
    {
        FileModel? file;
        try
        {
            file = JsonSerializer.Deserialize<FileModel>(json, Options);
        }
        catch (JsonException e)
        {
            throw new InvalidDataException($"The block layout file is not valid: {e.Message}", e);
        }

        if (file?.Name is null || file.Fields is null) throw new InvalidDataException("The block layout file needs a name and a list of fields");
        try
        {
            return new BlockLayout(file.Name, file.Size, [.. file.Fields.Select(f => new BlockField(f.Name ?? "", f.Offset, f.Type, f.Evidence ?? ""))]);
        }
        catch (ArgumentException e)
        {
            throw new InvalidDataException(e.Message, e);
        }
    }

    private sealed class FileModel
    {
        public string? Name { get; set; }

        public int Size { get; set; }

        public List<FieldModel>? Fields { get; set; }
    }

    private sealed class FieldModel
    {
        public string? Name { get; set; }

        public int Offset { get; set; }

        public BlockFieldType Type { get; set; }

        public string? Evidence { get; set; }
    }
}
