namespace OwlRenderingSharp.Graph.Tests;

public class RenderPassTests
{
    [Theory]
    [InlineData(RenderPass.Material, "gsys_assign_material")]
    [InlineData(RenderPass.ZPrepass, "gsys_assign_zonly")]
    [InlineData(RenderPass.DepthShadow, "gsys_assign_depthshadow")]
    [InlineData(RenderPass.XluZPrepass, "gsys_assign_xlu_zprepass")]
    [InlineData(RenderPass.Cubemap, "gsys_assign_cubemap")]
    [InlineData(RenderPass.Visualize, "gsys_assign_visualize")]
    public void EachPassSelectsItsAssignTypeChoice(RenderPass pass, string choice)
    {
        Assert.Equal(choice, pass.AssignType());
    }

    [Fact]
    public void OnlyThePassesThatFillDepthWriteNoColour()
    {
        Assert.True(RenderPass.ZPrepass.DepthOnly());
        Assert.True(RenderPass.DepthShadow.DepthOnly());
        Assert.False(RenderPass.Material.DepthOnly());
        Assert.False(RenderPass.Cubemap.DepthOnly());
    }

    [Fact]
    public void TheForwardPlanFillsDepthBeforeShading()
    {
        Assert.Equal([RenderPass.ZPrepass, RenderPass.Material], PassPlan.Forward.Passes);
        Assert.Equal([RenderPass.Material], PassPlan.ColorOnly.Passes);
    }

    [Fact]
    public void TheStandaloneOptionsTurnTheShadowPrePassOff()
    {
        Assert.Equal("0", PassOptions.Standalone.Overrides["ns_enable_shadow_pre_pass"]);
        Assert.Empty(PassOptions.None.Overrides);
    }
}
