using System.Numerics;
using OwlRenderingSharp.Gpu;

namespace OwlRenderingSharp.Graph;

/// <summary>Draws a model into an offscreen target and reads the pixels back, for tools and tests that want to look at the result.</summary>
/// <remarks>
/// The target is an 8-bit RGBA colour texture with a 32-bit depth texture, cleared to the given colour first, and it is released before the call returns. The
/// pixels come back with the top row first. The model is placed at the origin unless a placement is given. With a <see cref="ShadowMap"/> the model first casts into
/// its cascades from the sun (<c>toSun</c>, out to <c>shadowDistance</c>) and the colour pass reads them.
/// </remarks>
public static class ModelSnapshot
{
    public static byte[] Render(GpuDevice device, ModelDrawer drawer, ModelResources model, Camera camera, int width, int height, Vector4 background, Matrix4x4? placement = null, ShadowMap? shadows = null, Vector3 toSun = default, float shadowDistance = 50f)
    {
        using GpuTexture color = new(device, "snapshot color", GpuFormat.Rgba8, width, height);
        using GpuTexture depth = new(device, "snapshot depth", GpuFormat.Depth32F, width, height);
        using GpuFramebuffer target = new(device, "snapshot", [color], depth);
        GpuCommands commands = new(device);
        Matrix4x4 world = placement ?? Matrix4x4.Identity;
        shadows?.Render(camera, (float)width / height, toSun, shadowDistance, light => drawer.Draw(model, light, world, PassPlan.Shadow));
        commands.UseZeroToOneDepth();
        commands.Target(target, width, height);
        commands.Clear(background.X, background.Y, background.Z, background.W);
        drawer.Draw(model, shadows?.Frame(camera, width, height) ?? new FrameInfo(camera, width, height), world);
        device.Check("ModelSnapshot.Render", model.Name);
        return TopRowFirst(color.Download<byte>(), width, height);
    }

    private static byte[] TopRowFirst(byte[] pixels, int width, int height)
    {
        byte[] flipped = new byte[pixels.Length];
        int row = width * 4;
        for (int y = 0; y < height; y++) Array.Copy(pixels, (height - 1 - y) * row, flipped, y * row, row);
        return flipped;
    }
}
