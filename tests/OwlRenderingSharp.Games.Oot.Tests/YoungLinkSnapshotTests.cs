using System.Numerics;
using OwlRenderingSharp.Bench;
using OwlRenderingSharp.Content;
using OwlRenderingSharp.Gpu;
using OwlRenderingSharp.Graph;

namespace OwlRenderingSharp.Games.Oot.Tests;

/// <summary>Draws Young Link, supersampled and smoothed as every snapshot is, from a real pack and shader archive when the machine has them: set OWL_OOT_PACK to a pack made with linear textures and OWL_OOT_SHADERS to the model shader archive.</summary>
public class YoungLinkSnapshotTests
{
    private const int Size = 256;

    [SkippableFact]
    public void YoungLinkDrawsSomethingOverTheBackground()
    {
        string? pack = Environment.GetEnvironmentVariable("OWL_OOT_PACK"), shaders = Environment.GetEnvironmentVariable("OWL_OOT_SHADERS");
        Skip.If(pack is null || shaders is null || !File.Exists(pack) || !File.Exists(shaders), "OWL_OOT_PACK and OWL_OOT_SHADERS do not name files");
        using HeadlessGl? gl = HeadlessGl.TryCreate();
        Skip.If(gl is null, $"No OpenGL 4.5 context: {HeadlessGl.UnavailableReason}");

        using ContentSet content = new(new CollectingDiagnostics());
        content.AddPack(pack!);
        content.AddShaders("Model", shaders!);
        byte[] pixels = gl!.Run(device =>
        {
            using RenderServices services = new(device, content);
            using ModelResources model = new ModelResourcesBuilder(services).Build("Player_Child");
            using ModelDrawer drawer = new(services, new OotBlockProfile());
            return ModelSnapshot.RenderSmooth(device, drawer, model, BindPoseCamera.Frame(model.Skeleton), Size, Size, new Vector4(0.2f, 0.3f, 0.4f, 1f));
        });

        // The corner is background; the smoothed image must also hold blended edge pixels between it and the model.
        int background = 0, edge = 0;
        for (int i = 0; i < pixels.Length; i += 4)
        {
            int distance = Math.Abs(pixels[i] - pixels[0]) + Math.Abs(pixels[i + 1] - pixels[1]) + Math.Abs(pixels[i + 2] - pixels[2]);
            if (distance <= 3) background++;
            else if (distance <= 40) edge++;
        }

        Assert.True(edge > 0, "no blended edge pixels: the supersample resolve or FXAA did not run");
        Assert.InRange(background, 1, Size * Size - Size);
    }
}
