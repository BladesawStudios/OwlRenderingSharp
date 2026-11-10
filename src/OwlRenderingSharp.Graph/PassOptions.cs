namespace OwlRenderingSharp.Graph;

/// <summary>The draw-time shader options the renderer sets for every program it selects, on top of what a material assigns.</summary>
/// <remarks>
/// A material names only some options; the rest are the renderer's to decide from what the frame provides. The shadow pre-pass option, for one, says the program
/// reads a screen-space shadow buffer an earlier pass wrote, so a renderer without such a pass selects the programs compiled without it. <see cref="Standalone"/> is that
/// choice: no pre-pass buffers.
/// </remarks>
public sealed record PassOptions(IReadOnlyDictionary<string, string> Overrides)
{
    public static PassOptions Standalone { get; } = new(new Dictionary<string, string> { ["ns_enable_shadow_pre_pass"] = "0" });

    public static PassOptions None { get; } = new(new Dictionary<string, string>());
}
