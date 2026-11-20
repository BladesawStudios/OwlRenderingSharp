using Silk.NET.OpenGL;

namespace OwlRenderingSharp.Gpu;

/// <summary>A linked GL program built from GLSL source per stage.</summary>
/// <remarks>
/// Building compiles each stage and links them; a failure throws a <see cref="GpuException"/> naming the program and the stage, with the driver's log and the
/// source lines it points at (see <see cref="ShaderLogExcerpt"/>). Stage objects are deleted once linked. Uniform blocks are bound to slots by name; <see cref="BindUniformBlock"/> treats a name the program does not declare as an
/// error, and <see cref="TryBindUniformBlock"/> reports it, since the compiler removes blocks the code never reads.
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
        string? cached = BinaryFile(device, sources);
        if (cached is not null && TryLoad(gl, cached)) return;
        try
        {
            foreach ((GpuShaderStage stage, string source) in sources)
            {
                uint shader = Compile(gl, name, stage, source);
                stages.Add(shader);
                gl.AttachShader(_handle, shader);
            }

            if (cached is not null) gl.ProgramParameter(_handle, ProgramParameterPName.BinaryRetrievableHint, 1);
            Link(gl, name);
            if (cached is not null) Save(gl, cached);
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

    // The file a program of these sources is kept in: named by the driver and the sources, so a different driver or different code never finds it.
    private static string? BinaryFile(GpuDevice device, IReadOnlyDictionary<GpuShaderStage, string> sources)
    {
        if (device.ProgramCacheFolder is not { } folder) return null;
        System.Text.StringBuilder text = new(device.Identity);
        foreach ((GpuShaderStage stage, string source) in sources.OrderBy(s => s.Key)) text.Append('\n').Append((int)stage).Append('\n').Append(source);
        string hash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(text.ToString())))[..32];
        return Path.Combine(folder, hash + ".bin");
    }

    // Loads a kept binary into the program; false when there is none or the driver does not accept it, which leaves the program empty for the sources to be compiled.
    private unsafe bool TryLoad(GL gl, string file)
    {
        try
        {
            if (!File.Exists(file)) return false;
            byte[] data = File.ReadAllBytes(file);
            if (data.Length <= 4) return false;
            GLEnum format = (GLEnum)BitConverter.ToUInt32(data, 0);
            fixed (byte* binary = &data[4]) gl.ProgramBinary(_handle, format, binary, (uint)(data.Length - 4));
            gl.GetProgram(_handle, ProgramPropertyARB.LinkStatus, out int status);
            if (status != 0) return true;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { }

        // The driver took it badly: start the program again, and drop the file.
        gl.GetError();
        gl.DeleteProgram(_handle);
        _handle = gl.CreateProgram();
        try { File.Delete(file); }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { }
        return false;
    }

    private unsafe void Save(GL gl, string file)
    {
        try
        {
            // An error left in the queue by something earlier is not this call's.
            for (int i = 0; i < 32 && gl.GetError() != GLEnum.NoError; i++) { }
            gl.GetProgram(_handle, ProgramPropertyARB.ProgramBinaryLength, out int length);
            if (length <= 0) return;
            byte[] data = new byte[length + 4];
            GLEnum format;
            uint written;
            fixed (byte* binary = &data[4]) gl.GetProgramBinary(_handle, (uint)length, out written, out format, binary);
            if (written == 0 || gl.GetError() != GLEnum.NoError) return;
            BitConverter.TryWriteBytes(data.AsSpan(0, 4), (uint)format);
            Directory.CreateDirectory(Path.GetDirectoryName(file)!);
            string temporary = file + "." + Environment.ProcessId + ".tmp";
            File.WriteAllBytes(temporary, data.AsSpan(0, (int)written + 4).ToArray());
            File.Move(temporary, file, true);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { }
    }

    public string Name { get; }

    public uint Handle => _handle;

    public void BindUniformBlock(string blockName, uint slot)
    {
        if (!TryBindUniformBlock(blockName, slot)) throw new GpuException($"The program declares no uniform block {blockName}", resource: Name);
    }

    // The compiler drops a block the code never reads; this reports that instead of failing, for callers that bind every block a program declares.
    public bool TryBindUniformBlock(string blockName, uint slot)
    {
        uint index = _device.Gl.GetUniformBlockIndex(_handle, blockName);
        if (index == uint.MaxValue) return false;
        _device.Gl.UniformBlockBinding(_handle, index, slot);
        _device.Check("GpuProgram.BindUniformBlock", Name);
        return true;
    }

    // The sampler uniforms the linked program kept, each with the texture unit its binding qualifier fixes.
    public IReadOnlyList<GpuSamplerUniform> Samplers()
    {
        GL gl = _device.Gl;
        gl.GetProgram(_handle, ProgramPropertyARB.ActiveUniforms, out int count);
        List<GpuSamplerUniform> samplers = [];
        for (uint i = 0; i < count; i++)
        {
            string name = gl.GetActiveUniform(_handle, i, out _, out UniformType type);
            GpuSamplerKind? kind = KindOf(type);
            if (kind is null) continue;
            gl.GetUniform(_handle, gl.GetUniformLocation(_handle, name), out int unit);
            samplers.Add(new GpuSamplerUniform(name, kind.Value, unit));
        }

        _device.Check("GpuProgram.Samplers", Name);
        return samplers;
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

    private static GpuSamplerKind? KindOf(UniformType type) => type switch
    {
        UniformType.Sampler2D or UniformType.IntSampler2D or UniformType.UnsignedIntSampler2D => GpuSamplerKind.Texture2D,
        UniformType.Sampler2DShadow => GpuSamplerKind.Texture2DShadow,
        UniformType.Sampler2DArray or UniformType.IntSampler2DArray or UniformType.UnsignedIntSampler2DArray => GpuSamplerKind.Texture2DArray,
        UniformType.Sampler3D or UniformType.IntSampler3D or UniformType.UnsignedIntSampler3D => GpuSamplerKind.Texture3D,
        UniformType.SamplerCube or UniformType.IntSamplerCube or UniformType.UnsignedIntSamplerCube => GpuSamplerKind.TextureCube,
        UniformType.Sampler1D or UniformType.Sampler1DShadow or UniformType.Sampler1DArray or UniformType.SamplerCubeShadow or UniformType.Sampler2DArrayShadow
            or UniformType.Sampler2DRect or UniformType.Sampler2DRectShadow or UniformType.SamplerBuffer => GpuSamplerKind.Other,
        _ => null,
    };

    private static ShaderType Type(GpuShaderStage stage) => stage switch
    {
        GpuShaderStage.Vertex => ShaderType.VertexShader,
        GpuShaderStage.Geometry => ShaderType.GeometryShader,
        GpuShaderStage.Fragment => ShaderType.FragmentShader,
        GpuShaderStage.Compute => ShaderType.ComputeShader,
        _ => throw new ArgumentOutOfRangeException(nameof(stage)),
    };
}
