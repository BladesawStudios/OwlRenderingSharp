using OwlRenderingSharp.Blocks;
using OwlRenderingSharp.Content.Textures;
using BfShaderSharp.Pack.Records;
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
        foreach (ShapeResources shape in model.Shapes) Console.WriteLine($"{shape.Name}: {model.Materials[shape.MaterialIndex].Name} program {model.Materials[shape.MaterialIndex].Program.Index} -> material {model.Materials[shape.MaterialIndex].Pass(RenderPass.Material, shape.Record.SkinCount)?.Program.Index} zonly {model.Materials[shape.MaterialIndex].Pass(RenderPass.ZPrepass, shape.Record.SkinCount)?.Program.Index} shadow {model.Materials[shape.MaterialIndex].Pass(RenderPass.DepthShadow, shape.Record.SkinCount)?.Program.Index} weight {shape.Record.SkinCount} blend {model.Materials[shape.MaterialIndex].Blends} bone {shape.BoneIndex} {model.Skeleton.Bones[shape.BoneIndex].Name} skin {shape.Record.SkinBones.Count}");

    if (Option("--material") is { } part)
        foreach (var reference in services.Content.FindModel(modelName)!.Materials.Where(m => m.Name.Contains(part)))
        {
            MaterialRecord record = services.Content.Material(reference.Material);
            Console.WriteLine($"== {reference.Name} program {record.Program} flags {record.Flags:X} state {record.RenderStateMode}/{record.BlendMode}");
            foreach (var a in record.SamplerAssigns) Console.WriteLine($"  assign {a.Key} -> {a.Value}");
            foreach (var sm in record.Samplers) Console.WriteLine($"  sampler {sm.Name} tex {sm.Texture} wrap {sm.WrapU}{sm.WrapV} filter {sm.FilterFlags:X} bias {sm.LodBias}");
            foreach (var r in record.RenderInfos) Console.WriteLine($"  info {r.Name} kind {r.Kind} i[{string.Join(",", r.Ints)}] f[{string.Join(",", r.Floats)}] s[{string.Join(",", r.Strings)}]");
            if (record.Block != default)
            {
                byte[] block = services.Content.Bytes(record.Block);
                var layoutRecord = services.Content.UniformLayout(record.Layout);
                Console.WriteLine($"  block {block.Length} bytes, layout {layoutRecord.Name} {layoutRecord.Size}");
                foreach (var u in layoutRecord.Uniforms.OrderBy(u => u.Offset))
                    Console.WriteLine($"    @{u.Offset} (slot {u.Offset / 16}.{u.Offset % 16 / 4}) {u.Name} = {(u.Offset + 4 <= block.Length ? System.Buffers.Binary.BinaryPrimitives.ReadSingleLittleEndian(block.AsSpan(u.Offset)) : float.NaN)}");
            }

            foreach (var prm in Array.Empty<ParamRecord>())
            {
                int n = prm.Type switch { 0 => 1, 1 => 2, 2 => 3, 3 => 4, _ => 4 };
                var floats = Enumerable.Range(0, Math.Min(n, 4)).Select(i => System.Buffers.Binary.BinaryPrimitives.ReadSingleLittleEndian(record.ParamData.Span[(prm.Offset + i * 4)..]));
                Console.WriteLine($"  param {prm.Name} type {prm.Type} @{prm.Offset} [{string.Join(",", floats)}]");
            }
        }

    if (Option("--texture") is { } texName && services.Content.FindTexture(texName) is { } texRecord)
    {
        var decoded = services.Content.DecodeTexture(texRecord);
        Console.WriteLine($"{texName}: format {texRecord.Format:X} {decoded.Width}x{decoded.Height} {decoded.Encoding} mips {decoded.MipCount}");
        if (decoded.Encoding is TextureEncoding.Rgba8 or TextureEncoding.Rgba8Srgb) PngWriter.Write(Option("--texture-out") ?? "texture.png", decoded.Width, decoded.Height, decoded.Levels[0]);
    }

    if (args.Contains("--attribs"))
        foreach (ShapeResources shape in model.Shapes.Where(sh => sh.Name.Contains(Option("--only") ?? "")))
            Console.WriteLine($"{shape.Name} verts {shape.Record.VertexCount} skin {shape.Record.SkinCount} radii [{string.Join(",", shape.Record.Radii)}] bounds [{string.Join(",", shape.Record.BoundingRadii)}] attribs {string.Join(" ", shape.Record.Attribs.Select(a => $"{a.Name}:{a.Format:X}@{a.Offset}/{a.Stream}"))}");

    if (args.Contains("--check"))
    {
        var sk = model.Skeleton;
        var w = BindPose.Worlds(sk);
        Console.WriteLine($"matrices {sk.MatrixToBone.Count} inverse {sk.InverseMatrices.Count} bones {sk.Bones.Count} flags {sk.Flags:X}");
        for (int k = 0; k < sk.InverseMatrices.Count; k++)
        {
            int i = sk.MatrixToBone[k];
            var b = sk.Bones[i];
            var m = sk.InverseMatrices[k];
            var inverse = new System.Numerics.Matrix4x4(m[0], m[4], m[8], 0, m[1], m[5], m[9], 0, m[2], m[6], m[10], 0, m[3], m[7], m[11], 1);
            var id = w[i] * inverse;
            double dev = Math.Abs(id.M11 - 1) + Math.Abs(id.M22 - 1) + Math.Abs(id.M33 - 1) + Math.Abs(id.M41) + Math.Abs(id.M42) + Math.Abs(id.M43);
            if (dev > 0.02) Console.WriteLine($"{i} {b.Name} parent {b.Parent} dev={dev:F3} s=({b.Scale[0]:F2},{b.Scale[1]:F2},{b.Scale[2]:F2})");
        }
    }

    OotBlockProfile profile = new(new OotBlockLayouts(), Environment());
    IBlockProfile active = profile;
    if (Option("--env-slots") is { } range)
    {
        string[] ends = range.Split('-');
        float v = float.Parse(Option("--env-value") ?? "1");
        active = new EnvironmentProbe(profile, int.Parse(ends[0]), int.Parse(ends[^1]), new Vector4(v));
    }

    if (Option("--ablate") is { } ablateBlock)
    {
        Ablate(device, services, model, profile, ablateBlock, float.Parse(Option("--ablate-value") ?? "0"), Option("--only") ?? "");
        return [];
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

    ModelResources shown = Option("--only") is { } only ? new ModelResources(model.Name, model.Skeleton, [.. model.Shapes.Where(x => only.Split(',').Any(part => x.Name.Contains(part)))], model.Materials) : model;
    return ModelSnapshot.Render(device, drawer, shown, camera, width, height, new Vector4(0.2f, 0.3f, 0.4f, 1f));
}

// With --romfs and --place the lighting is the place's palette at the given time of day; otherwise a neutral environment.
OotEnvironment Environment()
{
    if (Option("--romfs") is not { } romfs || Option("--place") is not { } place) return OotEnvironment.Neutral;
    string[] time = (Option("--time") ?? "12:00").Split(':');
    return OotEnvironment.At(EnvPaletteLoader.Load(romfs), place, int.Parse(time[0]), int.Parse(time[1]));
}

// Sets each field of a block to a value in turn and prints the total brightness of the shapes that are not background, so a field that changes the picture shows up.
void Ablate(GpuDevice device, RenderServices services, ModelResources model, OotBlockProfile profile, string block, float value, string only)
{
    Camera camera = BindPoseCamera.Frame(model.Skeleton);
    ModelResources shown = new(model.Name, model.Skeleton, [.. model.Shapes.Where(x => x.Name.Contains(only))], model.Materials);
    long Whites(IBlockProfile p)
    {
        using ModelDrawer drawer = new(services, p);
        byte[] pixels = ModelSnapshot.Render(device, drawer, shown, camera, 256, 256, new Vector4(0.2f, 0.3f, 0.4f, 1f));
        long sum = 0;
        for (int i = 0; i < pixels.Length; i += 4) if (!(pixels[i] == 51 && pixels[i + 1] == 77)) sum += pixels[i] + pixels[i + 1] + pixels[i + 2];
        return sum;
    }

    long baseline = Whites(profile);
    Console.WriteLine($"baseline {baseline}");
    foreach (BlockField field in new OotBlockLayouts().Find(block)!.Fields.Where(f => f.Type == BlockFieldType.Vector4))
    {
        long w = Whites(new AblationProfile(profile, block, field.Name, new Vector4(value)));
        if (w != baseline) Console.WriteLine($"{field.Name}: {w - baseline:+#;-#}");
    }
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
