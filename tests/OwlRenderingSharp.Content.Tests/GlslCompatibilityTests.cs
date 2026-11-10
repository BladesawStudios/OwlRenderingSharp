using OwlRenderingSharp.Content.Programs;

namespace OwlRenderingSharp.Content.Tests;

public class GlslCompatibilityTests
{
    private const string Separate = """
        layout (binding = 4) uniform sampler2D fp_t_tcb_A;
        layout (binding = 6) uniform texture2D fp_t2ds_cb0_40000000[32];
        layout (binding = 38) uniform sampler fp_s_cb0_40000000[32];
        void main() { x = texture(sampler2DShadow(fp_t2ds_cb0_40000000[int(a)], fp_s_cb0_40000000[int(b)]), vec3(1.0)); }
        """;

    [Fact]
    public void SeparateTextureAndSamplerArraysBecomeCombinedSamplers()
    {
        string result = GlslCompatibility.Rewrite(Separate);
        Assert.Contains("layout (binding = 6) uniform sampler2DShadow fp_t2ds_cb0_40000000[26];", result);
        Assert.Contains("texture(fp_t2ds_cb0_40000000[int(a)], vec3(1.0))", result);
        Assert.DoesNotContain("fp_s_cb0", result);
        Assert.Contains("uniform sampler2D fp_t_tcb_A;", result);
    }

    [Fact]
    public void SourceWithoutSeparateArraysIsReturnedAsItIs()
    {
        const string plain = "layout (binding = 4) uniform sampler2D fp_t_tcb_A;\nvoid main() { }";
        Assert.Same(plain, GlslCompatibility.Rewrite(plain));
    }
}
