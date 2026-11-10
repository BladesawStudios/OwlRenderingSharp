namespace OwlRenderingSharp.Core.Tests;

public class CollectingDiagnosticsTests
{
    [Fact]
    public void ItKeepsEveryDiagnosticInOrder()
    {
        CollectingDiagnostics diagnostics = new();
        diagnostics.Report(new RenderDiagnostic(DiagnosticSeverity.Warning, "missing texture", Resource: "Link_Body"));
        diagnostics.Report(new RenderDiagnostic(DiagnosticSeverity.Error, "no program", Pass: "gbuffer"));
        Assert.Equal(["missing texture", "no program"], diagnostics.Snapshot().Select(d => d.Message));
    }

    [Fact]
    public void ASnapshotDoesNotChangeWhenMoreIsReported()
    {
        CollectingDiagnostics diagnostics = new();
        IReadOnlyList<RenderDiagnostic> before = diagnostics.Snapshot();
        diagnostics.Report(new RenderDiagnostic(DiagnosticSeverity.Info, "x"));
        Assert.Empty(before);
        Assert.Single(diagnostics.Snapshot());
    }

    [Fact]
    public void ManyThreadsMayReportAtOnce()
    {
        CollectingDiagnostics diagnostics = new();
        Parallel.For(0, 1000, i => diagnostics.Report(new RenderDiagnostic(DiagnosticSeverity.Info, i.ToString())));
        Assert.Equal(1000, diagnostics.Snapshot().Count);
    }
}
