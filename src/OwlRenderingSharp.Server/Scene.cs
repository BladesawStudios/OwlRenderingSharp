using System.Numerics;
using BfShaderSharp.Pack.Records;
using OwlRenderingSharp.Content;
using OwlRenderingSharp.Content.Geometry;
using OwlRenderingSharp.Games.Oot;
using OwlRenderingSharp.Gpu;
using OwlRenderingSharp.Graph;

namespace OwlRenderingSharp.Server;

/// <summary>The lighting controls of the pictures.</summary>
sealed record Look(float SunStrength, float KeyLight, float SunAzimuth, float SunElevation, float Reflections, float Exposure, float Wetness, float FogDensity, float Occlusion, float Subsurface);

/// <summary>Everything Owl holds for one opened model: its content, resources, light probes and drawer.</summary>
/// <remarks>Drawing is the Owl snapshot path: a neutral environment, a sky and sphere-harmonic probe, two key lights placed relative to the camera, ambient occlusion, subsurface, fog and depth of field, then the game's tone map and FXAA.</remarks>
sealed class Scene : IDisposable
{
    private readonly ContentSet _content;
    private readonly RenderServices _services;
    private readonly ModelResources _model;
    private readonly OotBlockProfile _profile;
    private readonly ModelDrawer _drawer;
    private SkyProbe _probe;
    private readonly GpuDevice _device;
    private SnapshotScene _scene;
    private PostSettings _post;
    private OotEnvironment _environment = OotEnvironment.Neutral;
    private float _keyPower, _wetness, _reflections = -1f;

    public Scene(GpuDevice device, CollectingDiagnostics diagnostics, string pack, string shaders, string name, Look look)
    {
        _device = device;
        _content = new ContentSet(diagnostics);
        _content.AddPack(pack);
        _content.AddShaders("Model", shaders);
        _services = new RenderServices(device, _content);
        _model = new ModelResourcesBuilder(_services).Build(name);
        OotEnvironment environment = _environment;
        _profile = new OotBlockProfile(new OotBlockLayouts(), environment);
        _drawer = new ModelDrawer(_services, _profile);

        (Vector3 skyForProbe, _) = OotLights.ProbeSky(environment, 0f, 0.7f);
        (_, Vector3 groundForProbe) = OotLights.SkyColors(environment);
        Vector3 up = skyForProbe * 0.3f, down = groundForProbe * 0.25f * 0.3f;
        _drawer.SetLightProbes(ShProbe.Entry(ShProbe.Project(d => SkyProbe.Blend(up, down, d))));

        _probe = BuildProbe(environment, look.Reflections);
        _post = OotPost.From(environment.Palette);
        _scene = new SnapshotScene(null, null, OotLights.ToSun(environment), 20f, _probe.Textures, 1f, 0.15f, ["Mt_Skin", "Mt_Face"], new SubsurfacePass.Settings(Strength: 0.5f), OotAtmosphere.Fog(environment.Palette, 1f), OotAtmosphere.Focus(2));
        _reflections = look.Reflections;
        _warmMaterials = [.. _model.Shapes.Select(s => s.MaterialIndex).Distinct().Order()];
        Apply(look);
    }

    // The sky the reflections see, scaled by the reflection strength: skin reflecting a full-strength sky glints in patches where its occlusion map is bright.
    private SkyProbe BuildProbe(OotEnvironment environment, float reflections)
    {
        (_, Vector3 ground) = OotLights.SkyColors(environment);
        (Vector3 probeSky, SkySun sun) = OotLights.ProbeSky(environment, 5f * reflections, 0.7f);
        return new SkyProbe(_services, probeSky * reflections, ground * 0.25f * reflections, sun, null);
    }

    // The controls that need no rebuild: the profile's sun, the wetness written into the materials, the light power, and the scene's occlusion, scatter and fog and the post chain's exposure.
    public void Apply(Look look)
    {
        if (look.SunAzimuth != _environment.SunAzimuth || look.SunElevation != _environment.SunElevation || look.Reflections != _reflections)
        {
            _environment = _environment with { SunAzimuth = look.SunAzimuth, SunElevation = look.SunElevation };
            _profile.Environment = _environment;
            _probe.Dispose();
            _probe = BuildProbe(_environment, look.Reflections);
            _reflections = look.Reflections;
            _scene = _scene with { ToSun = OotLights.ToSun(_environment), Textures = _probe.Textures };
        }

        _profile.SunScale = look.SunStrength;
        if (look.Wetness != _wetness)
        {
            _wetness = look.Wetness;
            _profile.Wetness = _wetness;
            WetnessControl.Apply(_model, _wetness);
        }

        _keyPower = look.KeyLight;
        _post = OotPost.From(_environment.Palette, look.Exposure);
        _scene = _scene with
        {
            OcclusionStrength = look.Occlusion,
            Subsurface = look.Subsurface > 0f ? new SubsurfacePass.Settings(Strength: look.Subsurface) : null,
            Fog = OotAtmosphere.Fog(_environment.Palette, look.FogDensity),
        };
    }

    /// <summary>Starts building every program the model's materials will ask for (in every pass and for each skin weight count of their shapes), so a driver that compiles in the background builds them all at once and the drawing that follows waits for the slowest, not for each in turn.</summary>
    public void Prefetch()
    {
        ModelRecord record = _content.FindModel(_model.Name) ?? throw new InvalidDataException($"The prepared pack has no model {_model.Name}.");
        PassProgramSelector selector = new(_content, PassOptions.Standalone);
        for (int m = 0; m < record.Materials.Count; m++)
        {
            MaterialRecord material = _content.Material(record.Materials[m].Material);
            HashSet<int> skins = [0];
            foreach (ShapeRef shape in record.Shapes.Where(s => s.MaterialIndex == m)) skins.Add(_content.Shape(shape.Shape).SkinCount);
            foreach (RenderPass pass in Enum.GetValues<RenderPass>())
                foreach (int skin in skins)
                    if (selector.Select(material, pass, skin) is >= 0 and int program) _services.Programs.Get(material.ShaderArchive, material.ShadingModel, program);
        }
    }

    private int _warmStep;
    private readonly int[] _warmMaterials;

    public bool Warming => _warmStep < _warmMaterials.Length;

    public int WarmDone => _warmStep;

    public int WarmTotal => _warmMaterials.Length;

    /// <summary>Draws one material's shapes into a tiny picture so the GPU compiles the programs for it, and the screen effects the first time.</summary>
    public void WarmStep(Camera camera)
    {
        int material = _warmMaterials[_warmStep++];
        ModelResources part = new(_model.Name, _model.Skeleton, [.. _model.Shapes.Where(s => s.MaterialIndex == material)], _model.Materials);
        Render(camera, 32, 32, 1, part);
    }

    private readonly Dictionary<int, (ModelResources Part, Camera Camera)?> _spheres = [];
    private readonly List<GpuGeometry> _sphereGeometry = [];
    private ModelResources? _visible;

    /// <summary>Draws only the named shapes (all when <paramref name="shapes"/> is null) and leaves out those of the hidden materials.</summary>
    public void Hide(IReadOnlySet<string> hiddenMaterials, IReadOnlySet<string>? shapes) =>
        _visible = hiddenMaterials.Count == 0 && (shapes is null || _model.Shapes.All(s => shapes.Contains(s.Name)))
            ? null
            : new ModelResources(_model.Name, _model.Skeleton, [.. _model.Shapes.Where(s => (shapes is null || shapes.Contains(s.Name)) && !hiddenMaterials.Contains(MaterialName(s.MaterialIndex)))], _model.Materials);

    // The prepared model names its materials "<model>/<material>"; the document knows them by the part after the slash.
    private string MaterialName(int index)
    {
        string full = _model.Materials[index].Name, prefix = _model.Name + "/";
        return full.StartsWith(prefix, StringComparison.Ordinal) ? full[prefix.Length..] : full;
    }

    /// <summary>The position in the prepared model of the material of this name, or -1.</summary>
    public int MaterialIndexOf(string name)
    {
        for (int i = 0; i < _model.Materials.Count; i++)
            if (MaterialName(i) == name) return i;
        return -1;
    }

    /// <summary>One material drawn alone, square: on a sphere in its own shader program, or as the shapes that use it (of <paramref name="shapes"/>, or all), from <paramref name="meshCamera"/>.</summary>
    public byte[] RenderMaterial(Camera? meshCamera, int size, int material, bool sphereShape, IReadOnlySet<string>? shapes)
    {
        if (sphereShape || meshCamera is null)
        {
            if (Sphere(material) is { } sphere) return Render(sphere.Camera, size, size, 2, sphere.Part);
            throw new InvalidOperationException("The model has no shape to put the material on.");
        }

        ModelResources part = new(_model.Name, _model.Skeleton, [.. _model.Shapes.Where(s => s.MaterialIndex == material && (shapes is null || shapes.Contains(s.Name)))], _model.Materials);
        return Render(meshCamera, size, size, 2, part);
    }

    // The sphere is built once for a material, from the geometry layout of the first shape that uses it, and drawn through the same material resources, so the programs are the ones already compiled.
    private (ModelResources Part, Camera Camera)? Sphere(int material)
    {
        if (_spheres.TryGetValue(material, out var built)) return built;
        ShapeResources? source = _model.Shapes.FirstOrDefault(s => s.MaterialIndex == material) ?? _model.Shapes.FirstOrDefault();
        if (source is null) return _spheres[material] = null;
        ProgramEntry program = _model.Materials[material].Program;
        ShapeGeometry template = ShapeGeometryBuilder.Build(source.Record, program.Attributes, key => _content.Bytes(key));
        const float radius = 0.5f;
        GpuGeometry geometry = new(_device, "preview sphere", PreviewSphere.Build(template, program.Attributes, radius));
        _sphereGeometry.Add(geometry);
        ShapeResources shape = new("preview sphere", source.Record, geometry, material, source.BoneIndex);
        int firstMatrix = 0;
        if (program.Attributes.TryGetValue("_i0", out int location) && template.Locations.ToList().IndexOf(location) is var slot and >= 0 && template.VertexCount > 0)
            firstMatrix = (int)BitConverter.ToUInt32(template.Vertices, slot * 16);
        SkeletonRecord skeleton = _model.Skeleton;
        Matrix4x4 placement = PreviewSphere.Placement(source.Record.SkinCount, source.BoneIndex, firstMatrix, BindPose.Worlds(skeleton), skeleton.InverseMatrices.Count, skeleton.MatrixToBone);
        Vector3 center = Vector3.Transform(Vector3.Zero, placement);
        float scale = MathF.Max(new Vector3(placement.M11, placement.M12, placement.M13).Length(), 1e-4f), size = radius * scale, fov = float.DegreesToRadians(30f), distance = size / MathF.Sin(fov / 2f) * 1.05f;
        Camera camera = new()
        {
            Eye = center + Vector3.Normalize(new Vector3(0.3f, 0.25f, 1f)) * distance,
            Target = center,
            Up = Vector3.UnitY,
            FovRadians = fov,
            NearPlane = MathF.Max(distance * 0.1f, 0.01f),
            FarPlane = distance + size * 3f,
        };
        return _spheres[material] = (new ModelResources("preview sphere", skeleton, [shape], _model.Materials), camera);
    }

    public byte[] Render(Camera camera, int width, int height, int supersample, ModelResources? only = null)
    {
        Vector3 forward = Vector3.Normalize(camera.Target - camera.Eye), right = Vector3.Normalize(Vector3.Cross(forward, Vector3.UnitY)), up = Vector3.Cross(right, forward);
        Vector3 white = Vector3.One * _keyPower;
        float d = Vector3.Distance(camera.Eye, camera.Target);
        _drawer.SetClusterLights([
            new ClusterLight(camera.Target - forward * d * 0.8f - right * d * 0.4f + up * d * 0.4f, white, 20f),
            new ClusterLight(camera.Target - forward * d * 0.8f + right * d * 0.5f - up * d * 0.1f, white * 0.3f, 20f)]);
        return ModelSnapshot.RenderSmooth(_device, _drawer, only ?? _visible ?? _model, camera, width, height, new Vector4(0f, 0f, 0f, 1f), _scene, _post, supersample, true, null, true);
    }

    public void Dispose()
    {
        foreach (GpuGeometry geometry in _sphereGeometry) geometry.Dispose();
        _probe.Dispose();
        _drawer.Dispose();
        _model.Dispose();
        _services.Dispose();
        _content.Dispose();
    }
}
