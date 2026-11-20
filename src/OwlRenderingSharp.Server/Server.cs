using System.Globalization;
using System.Numerics;
using System.Text;
using Silk.NET.Core.Contexts;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using OwlRenderingSharp.Gpu;
using OwlRenderingSharp.Graph;

namespace OwlRenderingSharp.Server;

/// <summary>Reads the commands of <see cref="Protocol"/> and draws the model they name.</summary>
sealed class Server(Stream input, Stream output) : IDisposable
{
    private readonly StreamReader _in = new(input, new UTF8Encoding(false));
    private readonly StreamWriter _lines = new(output, new UTF8Encoding(false)) { AutoFlush = true, NewLine = "\n" };
    private readonly Preparer _preparer = new();
    private readonly CollectingDiagnostics _diagnostics = new();
    private IWindow? _window;
    private GpuDevice? _device;
    private Scene? _scene;
    private Look _look = new(1f, 1f, 0f, 0.7f, 1f, 1f, 0f, 1f, 1f, 0.5f);
    private IReadOnlySet<string> _hiddenMaterials = new HashSet<string>();
    private IReadOnlySet<string>? _shapes;

    public int Run()
    {
        string? line;
        while ((line = _in.ReadLine()) is not null)
        {
            string[] part = line.TrimStart('﻿').Split(' ', 2);
            string rest = part.Length > 1 ? part[1] : "";
            try
            {
                switch (part[0])
                {
                    case "open": Open(rest.Split('\t')); break;
                    case "look": Apply(rest.Split(' ')); break;
                    case "hide": Hide(rest.Split('\t')); break;
                    case "render": Render(rest.Split(' ')); break;
                    case "preview": Preview(rest.Split('\t')); break;
                    case "quit": return 0;
                    default: throw new InvalidOperationException($"Unknown command '{part[0]}'.");
                }
            }
            catch (Exception exception) when (exception is not OutOfMemoryException)
            {
                _lines.WriteLine(Protocol.Fail + exception.Message.ReplaceLineEndings(" "));
                Console.Error.WriteLine(exception);
            }
        }

        return 0;
    }

    private void Status(string text) => _lines.WriteLine(Protocol.Status + text.ReplaceLineEndings(" "));

    private static float F(string text) => float.Parse(text, CultureInfo.InvariantCulture);

    private void Open(string[] a)
    {
        string romfs = a[0], cache = a[1], modelFile = a[3], romfsPaths = a.Length > 4 ? a[4] : "", edited = a.Length > 5 ? a[5] : "";
        (string pack, string shaders, string name) = _preparer.Prepare(romfs, cache, modelFile, romfsPaths, edited, Status);

        Status("Loading the model");
        _scene?.Dispose();
        _scene = null;
        _hiddenMaterials = new HashSet<string>();
        _shapes = null;
        _device ??= CreateDevice();
        _scene = new Scene(_device, _diagnostics, pack, shaders, name, _look);
        // The GPU compiles a shader the first time it is drawn, so each material is drawn once, small, before the model is reported ready.
        Camera camera = new() { Eye = new Vector3(0f, 0f, 3f), Target = Vector3.Zero, Up = Vector3.UnitY, FovRadians = float.DegreesToRadians(30f), NearPlane = 0.1f, FarPlane = 10f };
        while (_scene.Warming)
        {
            Status($"Compiling the game's shaders for your GPU ({_scene.WarmDone} of {_scene.WarmTotal})");
            _scene.WarmStep(camera);
        }

        _lines.WriteLine(Protocol.Ready);
    }

    // A hidden window with an OpenGL 4.5 context; the server's own thread is the GL thread.
    private GpuDevice CreateDevice()
    {
        Window.PrioritizeGlfw();
        _window = Window.Create(WindowOptions.Default with
        {
            IsVisible = false,
            Size = new Silk.NET.Maths.Vector2D<int>(64, 64),
            API = new GraphicsAPI(ContextAPI.OpenGL, ContextProfile.Core, ContextFlags.ForwardCompatible, new APIVersion(4, 5)),
        });
        _window.Initialize();
        return new GpuDevice(GL.GetApi((IGLContextSource)_window));
    }

    private Scene Scene => _scene ?? throw new InvalidOperationException("No model is open.");

    private void Apply(string[] p)
    {
        _look = new Look(F(p[0]), F(p[1]), F(p[2]), F(p[3]), F(p[4]), F(p[5]), F(p[6]), F(p[7]), F(p[8]), F(p[9]));
        _scene?.Apply(_look);
    }

    private void Hide(string[] p)
    {
        _hiddenMaterials = new HashSet<string>(p[0].Split('|', StringSplitOptions.RemoveEmptyEntries), StringComparer.Ordinal);
        _shapes = p.Length > 1 && p[1].Length > 0 ? new HashSet<string>(p[1].Split('|', StringSplitOptions.RemoveEmptyEntries), StringComparer.Ordinal) : null;
        Scene.Hide(_hiddenMaterials, _shapes);
    }

    private static Camera Eye(string[] p, int at) => new()
    {
        Eye = new Vector3(F(p[at]), F(p[at + 1]), F(p[at + 2])),
        Target = new Vector3(F(p[at + 3]), F(p[at + 4]), F(p[at + 5])),
        Up = at == 2 ? new Vector3(F(p[at + 6]), F(p[at + 7]), F(p[at + 8])) : Vector3.UnitY,
        FovRadians = float.DegreesToRadians(F(p[at + (at == 2 ? 9 : 6)])),
        NearPlane = MathF.Max(F(p[at + (at == 2 ? 10 : 7)]), 0.01f),
        FarPlane = F(p[at + (at == 2 ? 11 : 8)]),
    };

    private void Render(string[] p)
    {
        int width = int.Parse(p[0], CultureInfo.InvariantCulture), height = int.Parse(p[1], CultureInfo.InvariantCulture), samples = int.Parse(p[14], CultureInfo.InvariantCulture);
        Send(width, height, Scene.Render(Eye(p, 2), width, height, samples));
    }

    private void Preview(string[] p)
    {
        int size = int.Parse(p[1], CultureInfo.InvariantCulture), material = Scene.MaterialIndexOf(p[0]);
        if (material < 0) throw new InvalidOperationException($"The model has no material {p[0]}.");
        bool sphere = p[2] == "sphere";
        Send(size, size, Scene.RenderMaterial(sphere ? null : Eye([.. p[3].Split(' ')], 0), size, material, sphere, _shapes));
    }

    private void Send(int width, int height, byte[] rgba)
    {
        if (_diagnostics.Snapshot().Count(d => d.Severity == DiagnosticSeverity.Error) is > 0 and int errors) Console.Error.WriteLine($"{errors} renderer error(s)");
        _lines.WriteLine($"{Protocol.Frame}{width} {height}");
        output.Write(rgba, 0, rgba.Length);
        output.Flush();
    }

    public void Dispose()
    {
        _scene?.Dispose();
        _preparer.Dispose();
        _window?.Dispose();
    }
}
