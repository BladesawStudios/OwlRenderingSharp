using BfShaderSharp.Pack;
using BfShaderSharp.Pack.Records;

namespace OwlRenderingSharp.Content.Tests;

public sealed class ContentSetTests : IDisposable
{
    private readonly List<string> _files = [];
    private readonly CollectingDiagnostics _diagnostics = new();

    public void Dispose()
    {
        foreach (string file in _files) File.Delete(file);
    }

    // A pack holding one model named Link whose vertex count says which pack it came from.
    private string PackWithModel(uint vertexCount)
    {
        string path = Path.Combine(Path.GetTempPath(), $"owl-{Guid.NewGuid():N}.bfap");
        _files.Add(path);
        using FileStream stream = File.Create(path);
        PackWriter writer = new(stream);
        RecordWriter record = new();
        new ModelRecord("Link", 0, vertexCount, default, [], []).Write(record);
        writer.AddEntry(PackEntryKind.Model, "Link", writer.Add(record.ToArray(), BlobCodec.Raw));
        writer.Finish();
        return path;
    }

    [Fact]
    public void AModelIsFoundInThePackThatHoldsIt()
    {
        using ContentSet content = new(_diagnostics);
        content.AddPack(PackWithModel(10));
        Assert.Equal(10u, content.FindModel("Link")?.VertexCount);
        Assert.Null(content.FindModel("Zelda"));
    }

    [Fact]
    public void ThePackAddedLastShadowsTheEarlierOnes()
    {
        using ContentSet content = new(_diagnostics);
        content.AddPack(PackWithModel(10));
        content.AddPack(PackWithModel(20));
        Assert.Equal(20u, content.FindModel("Link")?.VertexCount);
    }

    [Fact]
    public void AMissingTextureIsReportedAndReturnsNull()
    {
        using ContentSet content = new(_diagnostics);
        content.AddPack(PackWithModel(10));
        Assert.Null(content.FindTexture("Link_Alb"));
        RenderDiagnostic diagnostic = Assert.Single(_diagnostics.Snapshot());
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
        Assert.Equal("Link_Alb", diagnostic.Resource);
    }

    [Fact]
    public void ATextureIsDecodedFromTheBlobItsRecordNames()
    {
        string path = Path.Combine(Path.GetTempPath(), $"owl-{Guid.NewGuid():N}.bfap");
        _files.Add(path);
        BlobKey key;
        using (FileStream stream = File.Create(path))
        {
            PackWriter writer = new(stream);
            key = writer.Add(new byte[16], BlobCodec.Raw);
            writer.Finish();
        }

        using ContentSet content = new(_diagnostics);
        content.AddPack(path);
        TextureRecord record = new(key, 0, 0x0b01, 2, 2, 1, 1, 1, TextureStorage.Linear, [16]);
        Assert.Equal(16, content.DecodeTexture(record).Levels[0].Length);
    }

    [Fact]
    public void AMissingBlobNamesTheKey()
    {
        using ContentSet content = new(_diagnostics);
        content.AddPack(PackWithModel(10));
        Assert.Throws<KeyNotFoundException>(() => content.Blob(new BlobKey(1, 2)));
    }

    [Fact]
    public void AShaderArchiveThatWasNotAddedIsAnError()
    {
        using ContentSet content = new(_diagnostics);
        Assert.Throws<KeyNotFoundException>(() => content.Shaders("Model"));
    }
}
