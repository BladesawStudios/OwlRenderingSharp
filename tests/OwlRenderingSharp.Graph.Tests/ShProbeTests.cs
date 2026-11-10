using System.Numerics;

namespace OwlRenderingSharp.Graph.Tests;

public class ShProbeTests
{
    [Fact]
    public void AUniformEnvironmentOfOneHasThreePointFiveInItsFirstCoefficientAndNothingElse()
    {
        Vector3[] sh = ShProbe.Project(_ => Vector3.One, 32);
        Assert.Equal(3.5449f, sh[0].X, 2);
        for (int k = 1; k < ShProbe.Coefficients; k++) Assert.True(sh[k].Length() < 0.02f);
    }

    [Fact]
    public void LightFromAboveFillsTheLinearTermOfTheUpAxis()
    {
        Vector3[] sh = ShProbe.Project(d => d.Y > 0f ? Vector3.One : Vector3.Zero, 32);
        Assert.True(sh[1].X > 0.5f);
        Assert.True(MathF.Abs(sh[3].X) < 0.02f);
    }

    [Fact]
    public void AnEntryHoldsNineColoursOfFourFloats()
    {
        Assert.Equal(ShProbe.EntryBytes / 4, ShProbe.Entry(new Vector3[ShProbe.Coefficients]).Length);
    }
}
