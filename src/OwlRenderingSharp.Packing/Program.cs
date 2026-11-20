using OwlRenderingSharp.Packing;

// Takes one command a line on standard input, its fields separated by tabs, and answers on standard output; it runs until the input closes or it is told to quit.
//   prepare <romfs> <cache folder> <model file> <edited file or empty> <romfs paths separated by |>
//     answers STAGE <what it is doing> lines, then RESULT <pack> <shaders> <name> (tab separated), or FAIL <message>, and always ends with END.
string? line;
while ((line = Console.In.ReadLine()) is not null)
{
    line = line.TrimStart('﻿');
    if (line.Length == 0) continue;
    string[] part = line.Split('\t');
    if (part[0] == "quit") return 0;
    try
    {
        if (part[0] != "prepare" || part.Length < 6) throw new InvalidOperationException($"Unknown command '{part[0]}'.");
        byte[]? edited = part[4].Length == 0 ? null : File.ReadAllBytes(part[4]);
        PreparedModel prepared = ModelPreparer.Prepare(part[1], part[2].Length > 0 ? part[2] : BfShaderSharp.RenderCache.Folder("owl"), part[3], part[5].Split('|', StringSplitOptions.RemoveEmptyEntries), edited, new Stages());
        Console.WriteLine($"RESULT {prepared.Pack}\t{prepared.Shaders}\t{prepared.Name}");
    }
    catch (Exception e) when (e is not OutOfMemoryException)
    {
        Console.WriteLine("FAIL " + e.Message.ReplaceLineEndings(" "));
        Console.Error.WriteLine(e);
    }

    Console.WriteLine("END");
    Console.Out.Flush();
}

return 0;

// Reports on the spot, not on another thread, so a stage is always said before the answer that follows it.
sealed class Stages : IProgress<string>
{
    public void Report(string value) => Console.WriteLine("STAGE " + value.ReplaceLineEndings(" "));
}
