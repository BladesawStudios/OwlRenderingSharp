using System.Numerics;
using OwlRenderingSharp;
using OwlRenderingSharp.Bench;
using OwlRenderingSharp.Content;
using OwlRenderingSharp.Content.Palettes;
using OwlRenderingSharp.Games.Oot;
using OwlRenderingSharp.Gpu;
using OwlRenderingSharp.Graph;
using OwlRenderingSharp.Snapshot;

// owl-snapshot <pack.bfap> <shaders.bfsp> <model> <out.png> [--size 1024x1024] [--eye x,y,z] [--target x,y,z] [--only shape-name-part] [--romfs dir --place KokiriForest --time 12:00]
// Draws one model in its rest pose into an offscreen target and writes the image. The camera frames the model's bones unless told otherwise.
if (args.Length < 4)
{
    Console.Error.WriteLine("usage: owl-snapshot <pack.bfap> <shaders.bfsp> <model> <out.png> [--size WxH] [--eye x,y,z] [--target x,y,z] [--only shape-name-part] [--romfs dir --place KokiriForest --time 12:00]");
    return 1;
}

(int width, int height) = Size(Option("--size") ?? "1024x1024");
CollectingDiagnostics diagnostics = new();
using ContentSet content = new(diagnostics);
content.AddPack(args[0]);
content.AddShaders("Model", args[1]);
using HeadlessGl? gl = HeadlessGl.TryCreate();
if (gl is null)
{
    Console.Error.WriteLine($"No OpenGL context: {HeadlessGl.UnavailableReason}");
    return 2;
}

byte[] rgba = gl.Run(device => Render(device, content, args[2], width, height));
PngWriter.Write(args[3], width, height, rgba);
foreach (RenderDiagnostic d in diagnostics.Snapshot()) Console.WriteLine($"{d.Severity}: {d.Message} [{d.Resource}]");
Console.WriteLine($"wrote {args[3]}");
return 0;

string? Option(string name)
{
    int at = Array.IndexOf(args, name);
    return at >= 0 && at + 1 < args.Length ? args[at + 1] : null;
}

static (int, int) Size(string text)
{
    string[] parts = text.Split('x');
    return (int.Parse(parts[0]), int.Parse(parts[1]));
}

static Vector3 Vector(string text)
{
    float[] v = [.. text.Split(',').Select(float.Parse)];
    return new Vector3(v[0], v[1], v[2]);
}

byte[] Render(GpuDevice device, ContentSet content, string modelName, int width, int height)
{
    using RenderServices services = new(device, content);
    using ModelResources model = new ModelResourcesBuilder(services).Build(modelName);
    if (args.Contains("--list"))
        foreach (ShapeResources shape in model.Shapes) Console.WriteLine($"{shape.Name}: {model.Materials[shape.MaterialIndex].Name} program {model.Materials[shape.MaterialIndex].Program.Index}");

    OotBlockProfile profile = new(new OotBlockLayouts(), Environment());
    IBlockProfile active = profile;
    if (Option("--env-slots") is { } range)
    {
        string[] ends = range.Split('-');
        float v = float.Parse(Option("--env-value") ?? "1");
        active = new EnvironmentProbe(profile, int.Parse(ends[0]), int.Parse(ends[^1]), new Vector4(v));
    }

    if (Option("--env-sweep") is { } sweep)
    {
        Sweep(device, services, model, profile, float.Parse(sweep));
        return [];
    }

    using ModelDrawer drawer = new(services, active);
    Camera camera = BindPoseCamera.Frame(model.Skeleton);
    if (Option("--eye") is { } eye) camera.Eye = Vector(eye);
    if (Option("--target") is { } aim) camera.Target = Vector(aim);

    ModelResources shown = Option("--only") is { } only ? new ModelResources(model.Name, model.Skeleton, [.. model.Shapes.Where(x => x.Name.Contains(only))], model.Materials) : model;
    return ModelSnapshot.Render(device, drawer, shown, camera, width, height, new Vector4(0.2f, 0.3f, 0.4f, 1f));
}

// With --romfs and --place the lighting is the place's palette at the given time of day; otherwise a neutral environment.
OotEnvironment Environment()
{
    if (Option("--romfs") is not { } romfs || Option("--place") is not { } place) return OotEnvironment.Neutral;
    string[] time = (Option("--time") ?? "12:00").Split(':');
    return OotEnvironment.At(EnvPaletteLoader.Load(romfs), place, int.Parse(time[0]), int.Parse(time[1]));
}

// Sets one environment slot at a time and prints the slots that change the picture, with how much brightness they add.
void Sweep(GpuDevice device, RenderServices services, ModelResources model, OotBlockProfile profile, float value)
{
    Camera camera = BindPoseCamera.Frame(model.Skeleton);
    Vector4 background = new(0.2f, 0.3f, 0.4f, 1f);
    long Brightness(IBlockProfile p)
    {
        using ModelDrawer drawer = new(services, p);
        byte[] pixels = ModelSnapshot.Render(device, drawer, model, camera, 128, 128, background);
        long sum = 0;
        for (int i = 0; i < pixels.Length; i += 4) sum += pixels[i] + pixels[i + 1] + pixels[i + 2];
        return sum;
    }

    long baseline = Brightness(new EnvironmentProbe(profile, 0, -1, Vector4.Zero));
    for (int slot = 0; slot < 114; slot++)
    {
        long sum = Brightness(new EnvironmentProbe(profile, slot, slot, new Vector4(value)));
        if (sum != baseline) Console.WriteLine($"slot {slot}: {sum - baseline:+#;-#}");
    }
}
