namespace OwlRenderingSharp;

/// <summary>How serious a diagnostic is.</summary>
/// <remarks>A warning is a fallback the renderer took and can show; an error is something that did not render.</remarks>
public enum DiagnosticSeverity
{
    Info,
    Warning,
    Error,
}

/// <summary>One thing the renderer wants a host to know: what happened, to which resource, in which pass.</summary>
/// <remarks>
/// A diagnostic names the pass and the resource it concerns when there is one, so a studio can show it beside the model or
/// target. Fallbacks such as a missing texture or a nearest-program lookup are reported as warnings.
/// </remarks>
public sealed record RenderDiagnostic(DiagnosticSeverity Severity, string Message, string? Pass = null, string? Resource = null);
