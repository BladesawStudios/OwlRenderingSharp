using OwlRenderingSharp.Gpu;

namespace OwlRenderingSharp.Graph;

/// <summary>The screen-space effects a snapshot runs over its lit image, in order: the fallback occlusion, skin scatter, distance fog and depth of field.</summary>
/// <remarks>
/// <see cref="Needed"/> says whether the scene asks for any; the caller then draws into a scratch image and hands it to <see cref="Run"/> with the depth of the view (and of the skin shapes alone for the
/// scatter), and the last effect writes the final image. The effects in between ping-pong through one scratch image. Each effect is its own class; this one only orders them. It
/// owns nothing between runs and is used from the GL thread only.
/// </remarks>
public sealed class ScreenEffects(GpuDevice device)
{
    public static bool Needed(SnapshotScene scene, bool hasChain, bool hasSkin) =>
        (scene.OcclusionStrength > 0f && !hasChain) || (scene.Subsurface is not null && hasSkin) || scene.Fog is not null || scene.DepthOfField is not null;

    public void Run(GpuTexture lit, GpuTexture viewDepth, GpuTexture? skinDepth, Camera camera, SnapshotScene scene, bool hasChain, GpuTexture target)
    {
        List<Action<GpuTexture, GpuFramebuffer>> steps = [];
        List<IDisposable> owned = [];
        try
        {
            if (scene.OcclusionStrength > 0f && !hasChain)
            {
                AmbientOcclusionPass pass = Own(new AmbientOcclusionPass(device), owned);
                steps.Add((src, dst) => pass.Run(src, viewDepth, camera, scene.OcclusionRadius, scene.OcclusionStrength, dst));
            }

            if (scene.Subsurface is not null && skinDepth is not null)
            {
                SubsurfacePass pass = Own(new SubsurfacePass(device), owned);
                steps.Add((src, dst) => pass.Run(src, viewDepth, skinDepth, camera, scene.Subsurface, dst));
            }

            if (scene.Fog is { } fog)
            {
                DepthFogPass pass = Own(new DepthFogPass(device), owned);
                steps.Add((src, dst) => pass.Run(src, viewDepth, camera, fog.Color, fog.Start, fog.End, dst));
            }

            if (scene.DepthOfField is { } focus)
            {
                DepthOfFieldPass pass = Own(new DepthOfFieldPass(device), owned);
                steps.Add((src, dst) => pass.Run(src, viewDepth, camera, focus.FocalDistance, focus.Range, focus.MaxRadius, dst));
            }

            GpuTexture current = lit;
            GpuTexture[] scratch = [];
            try
            {
                scratch = steps.Count > 1 ? [new GpuTexture(device, "effects a", lit.Format, lit.Width, lit.Height), new GpuTexture(device, "effects b", lit.Format, lit.Width, lit.Height)] : [];
                for (int i = 0; i < steps.Count; i++)
                {
                    GpuTexture destination = i == steps.Count - 1 ? target : scratch[i % 2];
                    using GpuFramebuffer framebuffer = new(device, "effects", [destination]);
                    steps[i](current, framebuffer);
                    current = destination;
                }
            }
            finally
            {
                foreach (GpuTexture texture in scratch) texture.Dispose();
            }
        }
        finally
        {
            foreach (IDisposable item in owned) item.Dispose();
        }
    }

    private static T Own<T>(T item, List<IDisposable> owned) where T : IDisposable
    {
        owned.Add(item);
        return item;
    }
}
