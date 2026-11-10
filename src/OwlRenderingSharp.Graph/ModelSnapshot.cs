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
    public static byte[] RenderSmooth(GpuDevice device, ModelDrawer drawer, ModelResources model, Camera camera, int width, int height, Vector4 background, SnapshotScene? scene = null, PostSettings? post = null, int supersample = 2, bool fxaa = true, GamePostChain? chain = null, bool transparent = false)
    {
        using GpuTexture hdr = new(device, "snapshot hdr", GpuFormat.Rgba16F, width * supersample, height * supersample);
        using GpuTexture? coverage = transparent ? new GpuTexture(device, "snapshot coverage", GpuFormat.Rgba8, hdr.Width, hdr.Height) : null;
        DrawInto(device, drawer, model, camera, hdr, background, scene, chain, coverage);
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
        GpuTexture finished = resolved;
        using GpuTexture smoothed = new(device, "snapshot smoothed", GpuFormat.Rgba8, width, height);
        if (fxaa)
        {
            using GpuFramebuffer smoothedTarget = new(device, "snapshot smoothed", [smoothed]);
            using (FxaaPass pass = new(device)) pass.Run(resolved, smoothedTarget);
            finished = smoothed;
        }

        if (coverage is null) return TopRowFirst(finished.Download<byte>(), width, height);
        using GpuTexture withAlpha = new(device, "snapshot alpha", GpuFormat.Rgba8, width, height);
        using GpuFramebuffer alphaTarget = new(device, "snapshot alpha", [withAlpha]);
        using (CoverageAlphaPass alpha = new(device)) alpha.Apply(finished, coverage, supersample, alphaTarget);
        return TopRowFirst(withAlpha.Download<byte>(), width, height);
    }

    private static T[] Draw<T>(GpuDevice device, ModelDrawer drawer, ModelResources model, Camera camera, int width, int height, Vector4 background, SnapshotScene? scene, GpuFormat format, Func<GpuTexture, T[]> read)
        where T : unmanaged
    {
        using GpuTexture color = new(device, "snapshot color", format, width, height);
        DrawInto(device, drawer, model, camera, color, background, scene);
        return TopRowFirst(read(color), width, height);
    }

    private static void DrawInto(GpuDevice device, ModelDrawer drawer, ModelResources model, Camera camera, GpuTexture color, Vector4 background, SnapshotScene? scene, GamePostChain? chain = null, GpuTexture? coverage = null)
    {
        scene ??= new SnapshotScene();
        int width = color.Width, height = color.Height;
        Matrix4x4 world = scene.Placement ?? Matrix4x4.Identity;
        GpuCommands commands = new(device);
        using GpuTexture viewDepth = new(device, "snapshot view depth", GpuFormat.Depth32F, width, height);
        RenderViewDepth(device, drawer, model, camera, color, viewDepth, world, commands);
        using ViewDepthBuffers views = new(device, width, height);
        views.Resolve(viewDepth, camera);
        if (coverage is not null)
        {
            using GpuFramebuffer maskTarget = new(device, "snapshot coverage", [coverage]);
            using CoverageAlphaPass mask = new(device);
            mask.Mask(viewDepth, maskTarget);
        }
        Dictionary<string, FrameTexture> textures = new(scene.Textures ?? new Dictionary<string, FrameTexture>());
        foreach ((string name, FrameTexture texture) in views.Textures) textures[name] = texture;
        // The game's screen-space occlusion goes in the red channel of the shadow prepass, where its shading programs read it.
        using GpuTexture? open = scene.OcclusionStrength > 0f && chain is not null ? chain.Occlusion(viewDepth, camera, new SsaoSettings(Radius: scene.OcclusionRadius)) : null;
        using ShadowPrepass? prepass = scene.Shadows is not null ? CastShadows(device, drawer, model, camera, viewDepth, scene, world, open) : open is not null ? OcclusionOnly(device, camera, viewDepth, open, scene.OcclusionStrength) : null;
        if (prepass is not null) textures[ShadowPrepass.SamplerName] = prepass.Texture;
        scene = scene with { Textures = textures };
        using GpuTexture depth = new(device, "snapshot depth", GpuFormat.Depth32F, width, height);
        ModelResources? skin = scene.Subsurface is null ? null : Skin(model, scene.SkinMaterials);
        bool effects = ScreenEffects.Needed(scene, chain is not null, skin is not null);
        using GpuTexture? lit = effects ? new GpuTexture(device, "snapshot lit", color.Format, width, height) : null;
        using GpuFramebuffer target = new(device, "snapshot", [lit ?? color], depth);
        commands.UseZeroToOneDepth();
        commands.Target(target, width, height);
        commands.Clear(background.X, background.Y, background.Z, background.W);
        drawer.Draw(model, scene.Frame(camera, width, height), world);
        device.Check("ModelSnapshot.Render", model.Name);
        if (!effects) return;
        using GpuTexture? skinDepth = skin is null ? null : new GpuTexture(device, "snapshot skin depth", GpuFormat.Depth32F, width, height);
        if (skin is not null) RenderViewDepth(device, drawer, skin, camera, color, skinDepth!, world, commands);
        new ScreenEffects(device).Run(lit!, viewDepth, skinDepth, camera, scene, chain is not null, color);
    }

    // The shapes whose material names mark them as skin, as a model of their own; null when there are none.
    private static ModelResources? Skin(ModelResources model, IReadOnlyList<string>? markers)
    {
        if (markers is null) return null;
        ShapeResources[] shapes = [.. model.Shapes.Where(s => markers.Any(m => model.Materials[s.MaterialIndex].Name.Contains(m, StringComparison.Ordinal)))];
        return shapes.Length == 0 ? null : new ModelResources(model.Name, model.Skeleton, shapes, model.Materials);
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
    private static ShadowPrepass CastShadows(GpuDevice device, ModelDrawer drawer, ModelResources model, Camera camera, GpuTexture viewDepth, SnapshotScene scene, Matrix4x4 world, GpuTexture? occlusion)
    {
        ShadowMap shadows = scene.Shadows!;
        shadows.Render(camera, (float)viewDepth.Width / viewDepth.Height, scene.ToSun, scene.ShadowDistance, light => drawer.Draw(model, light, world, PassPlan.Shadow));
        ShadowPrepass prepass = new(device, viewDepth.Width, viewDepth.Height);
        prepass.Resolve(viewDepth, camera, shadows, occlusion: occlusion, occlusionStrength: scene.OcclusionStrength);
        return prepass;
    }

    private static ShadowPrepass OcclusionOnly(GpuDevice device, Camera camera, GpuTexture viewDepth, GpuTexture occlusion, float strength)
    {
        ShadowPrepass prepass = new(device, viewDepth.Width, viewDepth.Height);
        prepass.Resolve(viewDepth, camera, occlusion, strength);
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
