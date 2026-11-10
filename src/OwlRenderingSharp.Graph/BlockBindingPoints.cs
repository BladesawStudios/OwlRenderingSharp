namespace OwlRenderingSharp.Graph;

/// <summary>Assigns each engine uniform block name one binding point for every program, so a block is bound once and read by all.</summary>
/// <remarks>
/// The decompiled programs number their constant buffers per stage, and two stages can put different blocks on the same GLSL binding, so the
/// numbers in the source are not used. A block gets the next free point the first time it is asked for, starting at 1; point 0 is the support buffer.
/// </remarks>
public sealed class BlockBindingPoints
{
    public const int SupportBuffer = 0;
    private readonly Dictionary<string, int> _points = new(StringComparer.Ordinal);

    public int Get(string gsysName)
    {
        if (!_points.TryGetValue(gsysName, out int point)) _points[gsysName] = point = _points.Count + 1;
        return point;
    }
}
