using System.Numerics;
using OwlRenderingSharp;
using OwlRenderingSharp.Bench;
using OwlRenderingSharp.Content;
using OwlRenderingSharp.Games.Oot;
using OwlRenderingSharp.Gpu;
using OwlRenderingSharp.Graph;
using OwlRenderingSharp.Snapshot;

// owl-snapshot <pack.bfap> <shaders.bfsp> <model> <out.png> [--size 1024x1024] [--eye x,y,z] [--target x,y,z] [--only shape-name-part]
// Draws one model in its rest pose into an offscreen target and writes the image. The camera frames the model's bones unless told otherwise.
if (args.Length < 4)
{
    Console.Error.WriteLine("usage: owl-snapshot <pack.bfap> <shaders.bfsp> <model> <out.png> [--size WxH] [--eye x,y,z] [--target x,y,z] [--only shape-name-part]");
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
    using ModelDrawer drawer = new(services, new OotBlockProfile());
    Camera camera = BindPoseCamera.Frame(model.Skeleton);
    if (Option("--eye") is { } eye) camera.Eye = Vector(eye);
    if (Option("--target") is { } aim) camera.Target = Vector(aim);

    ModelResources shown = Option("--only") is { } only ? new ModelResources(model.Name, model.Skeleton, [.. model.Shapes.Where(x => x.Name.Contains(only))], model.Materials) : model;
    return ModelSnapshot.Render(device, drawer, shown, camera, width, height, new Vector4(0.2f, 0.3f, 0.4f, 1f));
}
