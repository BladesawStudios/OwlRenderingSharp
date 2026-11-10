namespace OwlRenderingSharp;

/// <summary>An <see cref="IRenderDiagnostics"/> that keeps what it is given, for tests and for hosts that show diagnostics in a list.</summary>
/// <remarks>It is safe to report to from many threads; <see cref="Snapshot"/> copies what has been reported so far.</remarks>
public sealed class CollectingDiagnostics : IRenderDiagnostics
{
    private readonly object _lock = new();
    private readonly List<RenderDiagnostic> _items = [];

    public void Report(RenderDiagnostic diagnostic)
    {
        lock (_lock) _items.Add(diagnostic);
    }

    public IReadOnlyList<RenderDiagnostic> Snapshot()
    {
        lock (_lock) return [.. _items];
    }
}
