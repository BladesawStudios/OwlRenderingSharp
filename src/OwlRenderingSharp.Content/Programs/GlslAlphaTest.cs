using System.Text.RegularExpressions;

namespace OwlRenderingSharp.Content.Programs;

/// <summary>Adds the alpha test the Switch's output stage did in hardware to a decompiled fragment program.</summary>
/// <remarks>
/// The game's colour programs write their alpha and leave the cutout to the hardware alpha test its material asks for (<c>gsys_alpha_test_*</c>); the decompiled GLSL has no <c>discard</c> in them.
/// Every <c>return</c> of such a program becomes a call that compares the written alpha with the reference the renderer puts in the support buffer: the compare function in
/// <c>alpha_test</c> (0 off, then never, less, equal, less or equal, greater, not equal, greater or equal, always) and the reference as float bits in <c>tfe_offset.x</c>, a field the fragment programs do not read.
/// A program that already discards (the depth-only programs test against the material's own reference), has no colour output or has no support buffer is returned as it is.
/// </remarks>
public static partial class GlslAlphaTest
{
    public const uint Off = 0, Never = 1, Less = 2, Equal = 3, LessOrEqual = 4, Greater = 5, NotEqual = 6, GreaterOrEqual = 7, Always = 8;

    private const string Function = """
        void owl_alpha_test(float a)
        {
            uint f = support_buffer.alpha_test;
            if (f == 0u) return;
            float r = intBitsToFloat(support_buffer.tfe_offset.x);
            bool keep = f == 2u ? a < r : f == 3u ? a == r : f == 4u ? a <= r : f == 5u ? a > r : f == 6u ? a != r : f == 7u ? a >= r : f != 1u;
            if (!keep) discard;
        }

        """;

    public static string Apply(string fragment)
    {
        if (fragment.Contains("discard") || !fragment.Contains("out_attr0") || !fragment.Contains("support_buffer")) return fragment;
        int main = fragment.IndexOf("void main()", StringComparison.Ordinal);
        if (main < 0) return fragment;
        string body = Return().Replace(fragment[main..], "{ owl_alpha_test(out_attr0.w); return; }");
        return fragment[..main] + Function + body;
    }

    [GeneratedRegex(@"\breturn;")]
    private static partial Regex Return();
}
