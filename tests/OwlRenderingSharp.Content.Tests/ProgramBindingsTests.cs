using BfShaderSharp.Errors;
using OwlRenderingSharp.Content.Programs;

namespace OwlRenderingSharp.Content.Tests;

public class ProgramBindingsTests
{
    private const string Vertex = """
        layout (binding = 0, std140) uniform _support_buffer { uint alpha_test; } support_buffer;
        layout (binding = 4, std140) uniform _vp_c3 { vec4 data[4096]; } vp_c3;
        layout (binding = 7, std140) uniform _vp_c6 { vec4 data[4096]; } vp_c6;
        """;

    private const string Fragment = """
        layout (binding = 0, std140) uniform _support_buffer { uint alpha_test; } support_buffer;
        layout (binding = 4, std140) uniform _fp_c3 { vec4 data[4096]; } fp_c3;
        layout (binding = 7, std140) uniform _fp_c6 { vec4 data[4096]; } fp_c6;
        layout (binding = 0, std430) buffer _fp_s0 { uint data[]; } fp_s0;
        layout (binding = 0) uniform sampler2D fp_t_tcb_8;
        layout (binding = 10) uniform samplerCube fp_t_tcb_20;
        """;

    private static readonly Dictionary<(bool, int), string> BlockNames = new()
    {
        [(true, 0)] = "gsys_context", [(false, 0)] = "gsys_context", [(true, 3)] = "gsys_skeleton", [(false, 3)] = "gsys_environment",
    };

    private static readonly Dictionary<(bool, int), string> SamplerNames = new() { [(false, 0)] = "_a0", [(false, 12)] = "gsys_cube_map" };

    [Fact]
    public void TheInterfaceListsBlocksAndSamplersWithTheirBindings()
    {
        GlslInterface fragment = GlslInterface.Parse(Fragment);
        Assert.Equal(["_support_buffer", "_fp_c3", "_fp_c6", "_fp_s0"], fragment.Blocks.Select(b => b.Name));
        Assert.True(fragment.Blocks[3].IsStorage);
        Assert.Equal([("fp_t_tcb_8", 0, "sampler2D"), ("fp_t_tcb_20", 10, "samplerCube")], fragment.Samplers.Select(s => (s.Name, s.Binding, s.Type)));
        Assert.Empty(GlslInterface.Parse(null).Blocks);
    }

    [Fact]
    public void ABlockIsNamedByTheSlotItsConstantBufferNumberMinusThreeHolds()
    {
        ProgramBindings bindings = ProgramBindings.Resolve(GlslInterface.Parse(Vertex), GlslInterface.Parse(Fragment), BlockNames, SamplerNames);
        Assert.Equal(
            [("_vp_c3", true, "gsys_context"), ("_vp_c6", true, "gsys_skeleton"), ("_fp_c3", false, "gsys_context"), ("_fp_c6", false, "gsys_environment")],
            bindings.Blocks.Select(b => (b.GlslName, b.Vertex, b.GsysName)));
    }

    [Fact]
    public void ASamplerIsNamedByEightPlusTwiceItsSlot()
    {
        ProgramBindings bindings = ProgramBindings.Resolve(GlslInterface.Parse(Vertex), GlslInterface.Parse(Fragment), BlockNames, SamplerNames);
        Assert.Equal([("fp_t_tcb_8", "_a0", 0), ("fp_t_tcb_20", "gsys_cube_map", 10)], bindings.Samplers.Select(s => (s.GlslName, s.ShaderName, s.Unit)));
    }

    [Fact]
    public void StorageBlocksAndTheSupportBufferAreReportedApart()
    {
        ProgramBindings bindings = ProgramBindings.Resolve(GlslInterface.Parse(Vertex), GlslInterface.Parse(Fragment), BlockNames, SamplerNames);
        Assert.Equal(["_fp_s0"], bindings.StorageBlocks);
        Assert.True(bindings.UsesSupportBuffer);
        Assert.DoesNotContain(bindings.Blocks, b => b.GlslName == "_support_buffer");
    }

    [Fact]
    public void ASlotTheArchiveDoesNotNameIsMalformed()
    {
        Assert.Throws<BfShaderFormatException>(() => ProgramBindings.Resolve(GlslInterface.Parse(Vertex), GlslInterface.Parse(Fragment), new Dictionary<(bool, int), string>(), SamplerNames));
    }
}
