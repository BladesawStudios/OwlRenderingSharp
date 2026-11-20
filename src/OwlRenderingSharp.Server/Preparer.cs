using System.Diagnostics;

namespace OwlRenderingSharp.Server;

/// <summary>Runs owl-pack, the process that prepares models, started once and kept for the life of the server, so every model after the first is prepared by code that is already compiled.</summary>
sealed class Preparer : IDisposable
{
    private Process? _process;

    public static string Executable => Path.Combine(AppContext.BaseDirectory, "owl-pack", OperatingSystem.IsWindows() ? "owl-pack.exe" : "owl-pack");

    private Process Server()
    {
        if (_process is { HasExited: false } running) return running;
        if (!File.Exists(Executable)) throw new FileNotFoundException($"The model preparer is not beside the server ({Executable}).");
        _process = Process.Start(new ProcessStartInfo(Executable)
        {
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = new System.Text.UTF8Encoding(false),
            StandardInputEncoding = new System.Text.UTF8Encoding(false),
        }) ?? throw new InvalidOperationException("The model preparer did not start.");
        // Its diagnostics are read and dropped so a full pipe never stalls it.
        _process.ErrorDataReceived += (_, _) => { };
        _process.BeginErrorReadLine();
        return _process;
    }

    /// <summary>Prepares one model and returns its pack, shader archive and name.</summary>
    public (string Pack, string Shaders, string Name) Prepare(string romfs, string cache, string modelFile, string romfsPaths, string editedFile, Action<string> stage)
    {
        Process process = Server();
        try
        {
            process.StandardInput.Write(string.Join('\t', "prepare", romfs, cache, modelFile, editedFile, romfsPaths) + "\n");
            process.StandardInput.Flush();
            (string, string, string)? result = null;
            while (true)
            {
                string line = process.StandardOutput.ReadLine() ?? throw new IOException("The model preparer ended while it was working.");
                if (line == "END") break;
                if (line.StartsWith("STAGE ", StringComparison.Ordinal)) stage(line[6..]);
                else if (line.StartsWith("RESULT ", StringComparison.Ordinal))
                {
                    string[] field = line[7..].Split('\t');
                    result = (field[0], field[1], field[2]);
                }
                else if (line.StartsWith("FAIL ", StringComparison.Ordinal)) throw new InvalidDataException(line[5..]);
            }

            return result ?? throw new InvalidDataException("The model preparer finished without a result.");
        }
        catch (IOException)
        {
            Dispose();
            throw;
        }
    }

    public void Dispose()
    {
        try
        {
            if (_process is { HasExited: false } running)
            {
                running.StandardInput.Write("quit\n");
                running.StandardInput.Flush();
                if (!running.WaitForExit(2000)) running.Kill(true);
            }
        }
        catch (Exception e) when (e is IOException or InvalidOperationException) { }

        _process?.Dispose();
        _process = null;
    }
}
