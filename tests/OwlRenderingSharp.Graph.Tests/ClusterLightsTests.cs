using System.Numerics;
using OwlRenderingSharp.Graph;
using Xunit;

namespace OwlRenderingSharp.Graph.Tests;

public sealed class ClusterLightsTests
{
    [Fact]
    public void CellStoresCountMinusOneThenListOffset()
    {
        uint[] cells = ClusterLights.Cells(2);
        Assert.Equal(1u, cells[0]);
        Assert.Equal(1u, cells[1]);
        Assert.Equal(0u, cells[ClusterLights.ListBase]);
        Assert.Equal(1u, cells[ClusterLights.ListBase + 1]);
    }

    [Fact]
    public void RecordPutsColourPositionAndInverseRangeWhereTheProgramsReadThem()
    {
        float[] data = ClusterLights.Records([new ClusterLight(new Vector3(1, 2, 3), new Vector3(4, 5, 6), 10f), new ClusterLight(Vector3.Zero, Vector3.One, 5f)]);
        Assert.Equal(4f, data[8]);
        Assert.Equal(0.1f, data[11]);
        Assert.Equal(2f, data[13]);
        Assert.Equal(0.2f, data[24 + 11]);
    }
}
