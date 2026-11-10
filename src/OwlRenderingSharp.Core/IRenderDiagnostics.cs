namespace OwlRenderingSharp;

/// <summary>Where the renderer reports what it would otherwise have to log.</summary>
/// <remarks>
/// The host passes one in; the renderer never writes to the console or keeps a sink of its own. An implementation may be called from
/// any thread the renderer uses, so it must not assume the GL thread.
/// </remarks>
public interface IRenderDiagnostics
{
    void Report(RenderDiagnostic diagnostic);
}
