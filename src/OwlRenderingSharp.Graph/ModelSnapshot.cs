using System.Numerics;
using OwlRenderingSharp.Gpu;

namespace OwlRenderingSharp.Graph;

/// <summary>Draws a model into an offscreen target and reads the pixels back, for tools and tests that want to look at the result.</summary>
/// <remarks>
/// <see cref="Render"/> draws into an 8-bit RGBA colour texture and returns bytes with the light clipped at one; <see cref="RenderHdr"/> draws into 16-bit float and returns the linear
/// colour as floats, for a tone mapper. Both have a 32-bit depth texture, are cleared to the given colour first, and release the target before the call returns. The pixels come back with
/// the top row first. The model is placed at the origin unless the <see cref="SnapshotScene"/> says otherwise.
/// </remarks>
public static class ModelSnapshot
{
    public static byte[] Render(GpuDevice device, ModelDrawer drawer, ModelResources model, Camera camera, int width, int height, Vector4 background, SnapshotScene? scene = null) =>
        Draw(device, drawer, model, camera, width, height, background, scene, GpuFormat.Rgba8, t => t.Download<byte>());

    public static float[] RenderHdr(GpuDevice device, ModelDrawer drawer, ModelResources model, Camera camera, int width, int height, Vector4 background, SnapshotScene? scene = null) =>
        Draw(device, drawer, model, camera, width, height, background, scene, GpuFormat.Rgba16F, t => t.DownloadAsFloats());

    /// <summary>Draws at <paramref name="supersample"/> times the size, tone maps and averages down to the requested size, then smooths the edges with <see cref="FxaaPass"/> unless <paramref name="fxaa"/> is off.</summary>
    public static byte[] RenderSmooth(GpuDevice device, ModelDrawer drawer, ModelResources model, Camera camera, int width, int height, Vector4 background, SnapshotScene? scene = null, float exposure = 0.25f, int supersample = 2, bool fxaa = true)
    {
        using GpuTexture hdr = new(device, "snapshot hdr", GpuFormat.Rgba16F, width * supersample, height * supersample);
        DrawInto(device, drawer, model, camera, hdr, background, scene);
        using GpuTexture resolved = new(device, "snapshot resolved", GpuFormat.Rgba8, width, height);
        using GpuFramebuffer resolvedTarget = new(device, "snapshot resolved", [resolved]);
        using (ResolvePass resolve = new(device)) resolve.Run(hdr, supersample, exposure, resolvedTarget);
        if (!fxaa) return TopRowFirst(resolved.Download<byte>(), width, height);
        using GpuTexture smoothed = new(device, "snapshot smoothed", GpuFormat.Rgba8, width, height);
        using GpuFramebuffer smoothedTarget = new(device, "snapshot smoothed", [smoothed]);
        using (FxaaPass pass = new(device)) pass.Run(resolved, smoothedTarget);
        return TopRowFirst(smoothed.Download<byte>(), width, height);
    }

    private static T[] Draw<T>(GpuDevice device, ModelDrawer drawer, ModelResources model, Camera camera, int width, int height, Vector4 background, SnapshotScene? scene, GpuFormat format, Func<GpuTexture, T[]> read)
        where T : unmanaged
    {
        using GpuTexture color = new(device, "snapshot color", format, width, height);
        DrawInto(device, drawer, model, camera, color, background, scene);
        return TopRowFirst(read(color), width, height);
    }

    private static void DrawInto(GpuDevice device, ModelDrawer drawer, ModelResources model, Camera camera, GpuTexture color, Vector4 background, SnapshotScene? scene)
    {
        scene ??= new SnapshotScene();
        int width = color.Width, height = color.Height;
        using GpuTexture depth = new(device, "snapshot depth", GpuFormat.Depth32F, width, height);
        using GpuFramebuffer target = new(device, "snapshot", [color], depth);
        GpuCommands commands = new(device);
        Matrix4x4 world = scene.Placement ?? Matrix4x4.Identity;
        scene.Shadows?.Render(camera, (float)width / height, scene.ToSun, scene.ShadowDistance, light => drawer.Draw(model, light, world, PassPlan.Shadow));
        commands.UseZeroToOneDepth();
        commands.Target(target, width, height);
        commands.Clear(background.X, background.Y, background.Z, background.W);
        drawer.Draw(model, scene.Frame(camera, width, height), world);
        device.Check("ModelSnapshot.Render", model.Name);
    }

    private static T[] TopRowFirst<T>(T[] pixels, int width, int height)
    {
        T[] flipped = new T[pixels.Length];
        int row = width * 4;
        for (int y = 0; y < height; y++) Array.Copy(pixels, (height - 1 - y) * row, flipped, y * row, row);
        return flipped;
    }
}
