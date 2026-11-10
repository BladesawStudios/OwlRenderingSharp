using System.Text.RegularExpressions;

namespace OwlRenderingSharp.Content.Programs;

/// <summary>Rewrites decompiled GLSL that uses features OpenGL's GLSL lacks into forms it accepts.</summary>
/// <remarks>
/// A program that picks its textures and samplers by an index read from a constant buffer is decompiled with Vulkan-style separate arrays,
/// <c>texture2D t[32]</c> and <c>sampler s[32]</c>, combined at the use as <c>sampler2DShadow(t[i], s[j])</c>. OpenGL has no separate textures, so each use
/// becomes the element of an array of combined samplers named like the texture array, and the sampler array is dropped. The sampler a use paired is lost, since a
/// combined sampler takes the sampler object of its texture unit. The array is cut to fit the 32 texture units that drivers offer. A source without these arrays is returned as it is.
/// </remarks>
public static partial class GlslCompatibility
{
    private const int MaxTextureUnits = 32;

    public static string Rewrite(string glsl)
    {
        Dictionary<string, string> kinds = [];
        HashSet<string> samplerArrays = [];
        string rewritten = CombinedUse().Replace(glsl, match =>
        {
            kinds.TryAdd(match.Groups[2].Value, match.Groups[1].Value);
            samplerArrays.Add(match.Groups[4].Value);
            return $"{match.Groups[2].Value}[{match.Groups[3].Value}]";
        });
        if (kinds.Count == 0) return glsl;
        rewritten = TextureArray().Replace(rewritten, match =>
            kinds.TryGetValue(match.Groups[3].Value, out string? kind) ? Declare(kind, match) : match.Value);
        return SamplerArray().Replace(rewritten, match => samplerArrays.Contains(match.Groups[2].Value) ? "" : match.Value);
    }

    // The array is cut to the texture units that remain after its binding, since drivers allow 32 units and the decompiled array can be as long as 32 from a later unit.
    private static string Declare(string kind, Match match)
    {
        int binding = int.Parse(match.Groups[1].Value), length = Math.Min(int.Parse(match.Groups[4].Value), MaxTextureUnits - binding);
        return $"layout (binding = {binding}) uniform {kind} {match.Groups[3].Value}[{length}];";
    }

    [GeneratedRegex(@"(sampler\w+)\((\w+)\[([^\]]*)\],\s*(\w+)\[[^\]]*\]\)")]
    private static partial Regex CombinedUse();

    [GeneratedRegex(@"layout \(binding = (\d+)\) uniform (texture\w+) (\w+)\[(\d+)\];")]
    private static partial Regex TextureArray();

    [GeneratedRegex(@"layout \(binding = (\d+)\) uniform sampler (\w+)\[\d+\];\r?\n?")]
    private static partial Regex SamplerArray();
}
