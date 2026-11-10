using OwlRenderingSharp.Blocks;

namespace OwlRenderingSharp.Core.Tests;

public class BlockLayoutTests
{
    private static BlockField F(string name, int offset, BlockFieldType type = BlockFieldType.Float) => new(name, offset, type, "test");

    [Fact]
    public void FieldsAreFoundByNameAndKeptInOffsetOrder()
    {
        BlockLayout layout = new("b", 32, [F("second", 16, BlockFieldType.Vector4), F("first", 0)]);
        Assert.Equal(["first", "second"], layout.Fields.Select(f => f.Name));
        Assert.Equal(16, layout.Field("second").Offset);
        Assert.Throws<KeyNotFoundException>(() => layout.Field("third"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(20)]
    public void ASizeThatIsNotAPositiveMultipleOfSixteenIsRejected(int size)
    {
        Assert.Throws<ArgumentException>(() => new BlockLayout("b", size, []));
    }

    [Fact]
    public void AFieldThatDoesNotFitOverlapsOrIsMisalignedIsRejected()
    {
        Assert.Throws<ArgumentException>(() => new BlockLayout("b", 16, [F("v", 16, BlockFieldType.Vector4)]));
        Assert.Throws<ArgumentException>(() => new BlockLayout("b", 16, [F("a", 0, BlockFieldType.Vector4), F("b", 8)]));
        Assert.Throws<ArgumentException>(() => new BlockLayout("b", 32, [F("v", 8, BlockFieldType.Vector4)]));
        Assert.Throws<ArgumentException>(() => new BlockLayout("b", 16, [F("a", 0), F("a", 4)]));
    }

    [Fact]
    public void AFieldWithoutEvidenceIsRejected()
    {
        Assert.Throws<ArgumentException>(() => new BlockLayout("b", 16, [new BlockField("a", 0, BlockFieldType.Float, " ")]));
    }

    [Fact]
    public void ALayoutFileGivesTheSameLayout()
    {
        BlockLayout layout = BlockLayoutFile.Parse("""
            {"name": "gsys_x", "size": 64, "fields": [
              {"name": "view", "offset": 0, "type": "matrix3x4", "evidence": "WRS GsysContext"},
              {"name": "scale", "offset": 48, "type": "Float", "evidence": "read by model program 6183"}]}
            """);
        Assert.Equal(("gsys_x", 64), (layout.Name, layout.Size));
        Assert.Equal(BlockFieldType.Matrix3x4, layout.Field("view").Type);
        Assert.Equal(48, layout.Field("scale").Offset);
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("{}")]
    [InlineData("""{"name": "x", "size": 16, "fields": [{"name": "a", "offset": 0, "type": "float"}]}""")]
    [InlineData("""{"name": "x", "size": 16, "fields": [{"name": "a", "offset": 0, "type": "quaternion", "evidence": "e"}]}""")]
    public void AMalformedFileIsAnInvalidDataException(string json)
    {
        Assert.Throws<InvalidDataException>(() => BlockLayoutFile.Parse(json));
    }
}
