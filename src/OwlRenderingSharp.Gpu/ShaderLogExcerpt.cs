using System.Text;
using System.Text.RegularExpressions;

namespace OwlRenderingSharp.Gpu;

/// <summary>Turns a shader compiler's info log into a message that shows the source lines the log complains about.</summary>
/// <remarks>
/// Drivers print the line as <c>0(12)</c> (NVIDIA) or <c>0:12</c> (Mesa, Intel); both are recognised. Each distinct line is shown once with
/// two lines of context. A log with no recognisable line number is returned as it is.
/// </remarks>
public static partial class ShaderLogExcerpt
{
    private const int Context = 2;

    public static string Format(string log, string source)
    {
        string[] lines = source.Split('\n');
        SortedSet<int> flagged = [];
        foreach (Match match in LineNumber().Matches(log))
        {
            int line = int.Parse(match.Groups[1].Success ? match.Groups[1].Value : match.Groups[2].Value);
            if (line >= 1 && line <= lines.Length) flagged.Add(line);
        }

        if (flagged.Count == 0) return log.TrimEnd();
        StringBuilder text = new(log.TrimEnd());
        foreach (int line in flagged)
        {
            text.Append("\n--- line ").Append(line).Append('\n');
            for (int i = Math.Max(1, line - Context); i <= Math.Min(lines.Length, line + Context); i++)
                text.Append(i == line ? "> " : "  ").Append(i).Append(": ").Append(lines[i - 1].TrimEnd('\r')).Append('\n');
        }

        return text.ToString().TrimEnd();
    }

    [GeneratedRegex(@"\d+\((\d+)\)|\d+:(\d+)")]
    private static partial Regex LineNumber();
}
