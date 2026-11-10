using OwlRenderingSharp.Content.Programs;

namespace OwlRenderingSharp.Content.Tests;

public class GlslAlphaTestTests
{
    private const string Colour = "layout (std140) uniform _support_buffer { uint alpha_test; ivec4 tfe_offset; } support_buffer;\nlayout (location = 0) out vec4 out_attr0;\nvoid main()\n{\n    out_attr0 = vec4(1.0);\n    if (out_attr0.x > 2.0) { return; }\n    return;\n}\n";

    [Fact]
    public void EveryReturnOfAColourProgramTestsTheWrittenAlphaFirst()
    {
        string patched = GlslAlphaTest.Apply(Colour);
        Assert.Equal(2, patched.Split("owl_alpha_test(out_attr0.w); return;").Length - 1);
        Assert.True(patched.IndexOf("void owl_alpha_test", StringComparison.Ordinal) < patched.IndexOf("void main()", StringComparison.Ordinal));
        Assert.Contains("discard", patched);
    }

    [Fact]
    public void AProgramThatDiscardsAlreadyIsLeftAlone()
    {
        string own = Colour.Replace("return;\n}", "discard;\n}");
        Assert.Equal(own, GlslAlphaTest.Apply(own));
    }

    [Fact]
    public void AProgramWithNoColourOutputOrNoSupportBufferIsLeftAlone()
    {
        string noColour = Colour.Replace("out_attr0", "other");
        Assert.Equal(noColour, GlslAlphaTest.Apply(noColour));
        string noSupport = Colour.Replace("support_buffer", "other_buffer");
        Assert.Equal(noSupport, GlslAlphaTest.Apply(noSupport));
    }
}
