namespace OwlRenderingSharp.Graph.Tests;

public class BlockBindingPointsTests
{
    [Fact]
    public void ABlockKeepsItsPointAndPointsStartAfterTheSupportBuffer()
    {
        BlockBindingPoints points = new();
        int context = points.Get("gsys_context");
        Assert.Equal(1, context);
        Assert.Equal(2, points.Get("gsys_shape"));
        Assert.Equal(context, points.Get("gsys_context"));
        Assert.Equal(0, BlockBindingPoints.SupportBuffer);
    }
}
