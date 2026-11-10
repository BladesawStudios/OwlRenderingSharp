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
    public static byte[] RenderSmooth(GpuDevice device, ModelDrawer drawer, ModelResources model, Camera camera, int width, int height, Vector4 background, SnapshotScene? scene = null, PostSettings? post = null, int supersample = 2, bool fxaa = true, GamePostChain? chain = null)
    {
        using GpuTexture hdr = new(device, "snapshot hdr", GpuFormat.Rgba16F, width * supersample, height * supersample);
        DrawInto(device, drawer, model, camera, hdr, background, scene, chain);
        using GpuTexture resolved = new(device, "snapshot resolved", GpuFormat.Rgba8, width, height);
        using GpuFramebuffer resolvedTarget = new(device, "snapshot resolved", [resolved]);
        if (chain is null)
        {
            using ResolvePass resolve = new(device);
            resolve.Run(hdr, supersample, post ?? new PostSettings(), resolvedTarget);
        }
        else
        {
            using GpuTexture display = chain.Run(hdr, post ?? new PostSettings());
            using DownsamplePass downsample = new(device);
            downsample.Run(display, supersample, resolvedTarget);
        }
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

    private static void DrawInto(GpuDevice device, ModelDrawer drawer, ModelResources model, Camera camera, GpuTexture color, Vector4 background, SnapshotScene? scene, GamePostChain? chain = null)
    {
        scene ??= new SnapshotScene();
        int width = color.Width, height = color.Height;
        Matrix4x4 world = scene.Placement ?? Matrix4x4.Identity;
        GpuCommands commands = new(device);
        using GpuTexture viewDepth = new(device, "snapshot view depth", GpuFormat.Depth32F, width, height);
        RenderViewDepth(device, drawer, model, camera, color, viewDepth, world, commands);
        using ViewDepthBuffers views = new(device, width, height);
        views.Resolve(viewDepth, camera);
        Dictionary<string, FrameTexture> textures = new(scene.Textures ?? new Dictionary<string, FrameTexture>());
        foreach ((string name, FrameTexture texture) in views.Textures) textures[name] = texture;
        using ShadowPrepass? prepass = scene.Shadows is null ? null : CastShadows(device, drawer, model, camera, viewDepth, scene, world);
        if (prepass is not null) textures[ShadowPrepass.SamplerName] = prepass.Texture;
        scene = scene with { Textures = textures };
        using GpuTexture depth = new(device, "snapshot depth", GpuFormat.Depth32F, width, height);
        bool occlude = scene.OcclusionStrength > 0f;
        using GpuTexture? unoccluded = occlude ? new GpuTexture(device, "snapshot unoccluded", color.Format, width, height) : null;
        using GpuFramebuffer target = new(device, "snapshot", [unoccluded ?? color], depth);
        commands.UseZeroToOneDepth();
        commands.Target(target, width, height);
        commands.Clear(background.X, background.Y, background.Z, background.W);
        drawer.Draw(model, scene.Frame(camera, width, height), world);
        if (!occlude) return;
        using GpuFramebuffer occluded = new(device, "snapshot occluded", [color]);
        using AmbientOcclusionPass pass = new(device);
        if (chain is null)
        {
            pass.Run(unoccluded!, viewDepth, camera, scene.OcclusionRadius, scene.OcclusionStrength, occluded);
            return;
        }

        using GpuTexture open = chain.Occlusion(viewDepth, camera, new SsaoSettings(Radius: scene.OcclusionRadius));
        pass.Apply(unoccluded!, open, scene.OcclusionStrength, occluded);
        device.Check("ModelSnapshot.Render", model.Name);
    }

    // The depth image of the view: the opaque shapes drawn with no colour, into a scratch colour target.
    private static void RenderViewDepth(GpuDevice device, ModelDrawer drawer, ModelResources model, Camera camera, GpuTexture scratch, GpuTexture viewDepth, Matrix4x4 world, GpuCommands commands)
    {
        using GpuFramebuffer depthTarget = new(device, "snapshot view depth", [scratch], viewDepth);
        commands.UseZeroToOneDepth();
        commands.Target(depthTarget, scratch.Width, scratch.Height);
        commands.Clear(0f, 0f, 0f, 0f);
        drawer.Draw(model, new FrameInfo(camera, scratch.Width, scratch.Height), world, new PassPlan([RenderPass.ZPrepass]));
    }

    // Renders the cascades from the sun and resolves the screen-space shadow mask from them and the view depth.
    private static ShadowPrepass CastShadows(GpuDevice device, ModelDrawer drawer, ModelResources model, Camera camera, GpuTexture viewDepth, SnapshotScene scene, Matrix4x4 world)
    {
        ShadowMap shadows = scene.Shadows!;
        shadows.Render(camera, (float)viewDepth.Width / viewDepth.Height, scene.ToSun, scene.ShadowDistance, light => drawer.Draw(model, light, world, PassPlan.Shadow));
        ShadowPrepass prepass = new(device, viewDepth.Width, viewDepth.Height);
        prepass.Resolve(viewDepth, camera, shadows);
        return prepass;
    }

    private static T[] TopRowFirst<T>(T[] pixels, int width, int height)
    {
        T[] flipped = new T[pixels.Length];
        int row = width * 4;
        for (int y = 0; y < height; y++) Array.Copy(pixels, (height - 1 - y) * row, flipped, y * row, row);
        return flipped;
    }
}
