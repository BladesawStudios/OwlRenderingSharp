using Silk.NET.OpenGL;

namespace OwlRenderingSharp.Gpu;

/// <summary>The GL context a renderer draws with, and the error check every GPU operation ends in.</summary>
/// <remarks>
/// It is created by the host over a context that is current on the calling thread and is used only from that thread. Nothing in
/// the library holds a device statically; every resource is given the device it belongs to. <see cref="Check"/> drains the GL error queue
/// and throws one <see cref="GpuException"/> that lists all of it.
/// </remarks>
public sealed class GpuDevice(GL gl)
{
    private const int MaxDrain = 32;

    private readonly DrawStateApplier _applier = new(gl);

    public GL Gl { get; } = gl;

    /// <summary>A folder where linked programs are kept as the driver's own binaries, so a program built before is loaded instead of compiled. Null keeps none.</summary>
    /// <remarks>A binary only fits the driver and card that made it, so the file names carry them and a binary the driver refuses is compiled again.</remarks>
    public string? ProgramCacheFolder { get; set; }

    private bool? _parallel;

    /// <summary>True when the driver compiles and links in threads of its own (GL_KHR_parallel_shader_compile), which it is then told to use as many of as it likes.</summary>
    /// <remarks>
    /// With it a program is compiled and linked in the background from the moment it is asked for, and is waited for only when something first needs it, so a model's programs are all built at once.
    /// Without it each call returns when the work is done, which is how a program was always built. <see cref="GpuProgram"/> checks a program only when it is first used either way.
    /// </remarks>
    public unsafe bool ParallelCompile
    {
        get
        {
            if (_parallel is { } known) return known;
            bool found = false;
            Gl.GetInteger(GLEnum.NumExtensions, out int count);
            for (uint i = 0; i < count && !found; i++)
            {
                string name = Gl.GetStringS(StringName.Extensions, i);
                found = name is "GL_KHR_parallel_shader_compile" or "GL_ARB_parallel_shader_compile";
            }

            // OWL_PARALLEL_COMPILE=0 turns it off, for comparing.
            if (Environment.GetEnvironmentVariable("OWL_PARALLEL_COMPILE") == "0") found = false;
            if (found)
            {
                nint function = Gl.Context.TryGetProcAddress("glMaxShaderCompilerThreadsKHR", out nint khr) ? khr : Gl.Context.TryGetProcAddress("glMaxShaderCompilerThreadsARB", out nint arb) ? arb : 0;
                if (function != 0) ((delegate* unmanaged<uint, void>)function)(uint.MaxValue);
                else found = false;
            }

            Gl.GetError();
            return (_parallel = found).Value;
        }
    }

    private string? _identity;

    /// <summary>The driver and card, which a program binary is only good for.</summary>
    public string Identity => _identity ??= $"{Gl.GetStringS(StringName.Vendor)}|{Gl.GetStringS(StringName.Renderer)}|{Gl.GetStringS(StringName.Version)}";

    public void Apply(DrawState state) => _applier.Apply(state);

    public void Check(string? pass = null, string? resource = null)
    {
        List<GLEnum> errors = [];
        for (int i = 0; i < MaxDrain; i++)
        {
            GLEnum error = Gl.GetError();
            if (error == GLEnum.NoError) break;
            errors.Add(error);
        }

        if (errors.Count > 0) throw new GpuException($"GL error: {string.Join(", ", errors)}", pass, resource);
    }
}
