using System.Numerics;

namespace OwlRenderingSharp.Graph.Tests;

public class ScreenEffectsTests
{
    [Fact]
    public void ASceneAskingForNothingNeedsNoEffects() => Assert.False(ScreenEffects.Needed(new SnapshotScene(), hasChain: true, hasSkin: false));

    [Fact]
    public void FogAndDepthOfFieldEachNeedTheEffects()
    {
        Assert.True(ScreenEffects.Needed(new SnapshotScene(Fog: new SnapshotScene.FogSettings(Vector4.Zero, 0, 1)), true, false));
        Assert.True(ScreenEffects.Needed(new SnapshotScene(DepthOfField: new SnapshotScene.FocusSettings(10, 5, 4)), true, false));
    }

    [Fact]
    public void ScatterNeedsSkinShapesAndTheFallbackOcclusionNeedsNoChain()
    {
        SnapshotScene scatter = new(Subsurface: new SubsurfacePass.Settings());
        Assert.False(ScreenEffects.Needed(scatter, true, hasSkin: false));
        Assert.True(ScreenEffects.Needed(scatter, true, hasSkin: true));
        SnapshotScene occluded = new(OcclusionStrength: 1f);
        Assert.False(ScreenEffects.Needed(occluded, hasChain: true, hasSkin: false));
        Assert.True(ScreenEffects.Needed(occluded, hasChain: false, hasSkin: false));
    }
}
