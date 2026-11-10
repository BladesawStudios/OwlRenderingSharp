using System.Text.RegularExpressions;

namespace OwlRenderingSharp.Content.Programs;

/// <summary>The blocks and samplers one stage of a decompiled program declares, read from its GLSL text.</summary>
/// <remarks>
/// The decompiler writes every declaration as <c>layout (binding = N, std140) uniform _name</c> or <c>layout (binding = N) uniform type name;</c>, so the
/// interface is read from the source without compiling it. A stage with no source has an empty interface.
/// </remarks>
public sealed partial class GlslInterface
{
    public static GlslInterface Empty { get; } = new([], []);

    private GlslInterface(IReadOnlyList<GlslBlock> blocks, IReadOnlyList<GlslSampler> samplers)
    {
        Blocks = blocks;
        Samplers = samplers;
    }

    public IReadOnlyList<GlslBlock> Blocks { get; }

    public IReadOnlyList<GlslSampler> Samplers { get; }

    public static GlslInterface Parse(string? glsl)
    {
        if (glsl is null) return Empty;
        List<GlslBlock> blocks = [.. BlockPattern().Matches(glsl).Select(m => new GlslBlock(m.Groups[4].Value, int.Parse(m.Groups[1].Value), m.Groups[3].Value == "buffer"))];
        List<GlslSampler> samplers = [.. SamplerPattern().Matches(glsl).Select(m => new GlslSampler(m.Groups[3].Value, int.Parse(m.Groups[1].Value), m.Groups[2].Value))];
        return new GlslInterface(blocks, samplers);
    }

    [GeneratedRegex(@"layout \(binding = (\d+), (std140|std430)\) (uniform|buffer) (\w+)")]
    private static partial Regex BlockPattern();

    [GeneratedRegex(@"layout \(binding = (\d+)\) uniform (sampler\w+) (\w+);")]
    private static partial Regex SamplerPattern();
}
