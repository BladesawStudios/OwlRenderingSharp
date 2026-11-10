using Silk.NET.OpenGL;

namespace OwlRenderingSharp.Gpu;

/// <summary>A linked GL program built from GLSL source per stage.</summary>
/// <remarks>
/// Building compiles each stage and links them; a failure throws a <see cref="GpuException"/> naming the program and the stage, with the driver's log and the
/// source lines it points at (see <see cref="ShaderLogExcerpt"/>). Stage objects are deleted once linked. Uniform blocks are bound to slots by name; a name the
/// program does not declare is an error, since a renderer that binds a block the shader does not read has the wrong shader.
/// </remarks>
public sealed class GpuProgram : IDisposable
{
    private readonly GpuDevice _device;
    private uint _handle;

    public GpuProgram(GpuDevice device, string name, IReadOnlyDictionary<GpuShaderStage, string> sources)
    {
        if (sources.Count == 0) throw new GpuException("A program needs at least one stage", resource: name);
        _device = device;
        Name = name;
        GL gl = device.Gl;
        List<uint> stages = [];
        _handle = gl.CreateProgram();
        try
        {
            foreach ((GpuShaderStage stage, string source) in sources)
            {
                uint shader = Compile(gl, name, stage, source);
                stages.Add(shader);
                gl.AttachShader(_handle, shader);
            }

            Link(gl, name);
        }
        catch
        {
            gl.DeleteProgram(_handle);
            _handle = 0;
            throw;
        }
        finally
        {
            foreach (uint shader in stages)
            {
                if (_handle != 0) gl.DetachShader(_handle, shader);
                gl.DeleteShader(shader);
            }
        }
    }

    public string Name { get; }

    public uint Handle => _handle;

    public void BindUniformBlock(string blockName, uint slot)
    {
        uint index = _device.Gl.GetUniformBlockIndex(_handle, blockName);
        if (index == uint.MaxValue) throw new GpuException($"The program declares no uniform block {blockName}", resource: Name);
        _device.Gl.UniformBlockBinding(_handle, index, slot);
        _device.Check("GpuProgram.BindUniformBlock", Name);
    }

    public void Dispose()
    {
        if (_handle == 0) return;
        _device.Gl.DeleteProgram(_handle);
        _handle = 0;
    }

    private static uint Compile(GL gl, string name, GpuShaderStage stage, string source)
    {
        uint shader = gl.CreateShader(Type(stage));
        gl.ShaderSource(shader, source);
        gl.CompileShader(shader);
        gl.GetShader(shader, ShaderParameterName.CompileStatus, out int status);
        if (status != 0) return shader;
        string log = gl.GetShaderInfoLog(shader);
        gl.DeleteShader(shader);
        throw new GpuException($"The {stage} stage did not compile:\n{ShaderLogExcerpt.Format(log, source)}", resource: name);
    }

    private void Link(GL gl, string name)
    {
        gl.LinkProgram(_handle);
        gl.GetProgram(_handle, ProgramPropertyARB.LinkStatus, out int status);
        if (status == 0) throw new GpuException($"The program did not link:\n{gl.GetProgramInfoLog(_handle).TrimEnd()}", resource: name);
    }

    private static ShaderType Type(GpuShaderStage stage) => stage switch
    {
        GpuShaderStage.Vertex => ShaderType.VertexShader,
        GpuShaderStage.Geometry => ShaderType.GeometryShader,
        GpuShaderStage.Fragment => ShaderType.FragmentShader,
        GpuShaderStage.Compute => ShaderType.ComputeShader,
        _ => throw new ArgumentOutOfRangeException(nameof(stage)),
    };
}
