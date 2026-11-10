namespace OwlRenderingSharp.Gpu;

/// <summary>A GL error or a failed GPU operation, naming the pass and the resource it happened in.</summary>
/// <remarks>The message carries every GL error that was pending when the check ran, so a driver that queues several is reported whole.</remarks>
public sealed class GpuException(string message, string? pass = null, string? resource = null) : Exception(Describe(message, pass, resource))
{
    public string? Pass { get; } = pass;

    public string? Resource { get; } = resource;

    private static string Describe(string message, string? pass, string? resource) =>
        (pass, resource) switch
        {
            (null, null) => message,
            (_, null) => $"{message} (pass {pass})",
            (null, _) => $"{message} (resource {resource})",
            _ => $"{message} (pass {pass}, resource {resource})",
        };
}
