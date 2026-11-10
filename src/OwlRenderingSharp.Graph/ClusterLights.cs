namespace OwlRenderingSharp.Graph;

/// <summary>A point light of the game's clustered light buffer: a world position, a colour already scaled by intensity and the range at which it fades to nothing.</summary>
public readonly record struct ClusterLight(System.Numerics.Vector3 Position, System.Numerics.Vector3 Color, float Range);

/// <summary>Lays lights out the way the game's model programs read them from storage buffers 1 and 2.</summary>
/// <remarks>
/// Buffer 1 holds a cell table (two words per cell: the light count minus one, then the 1-based offset of the cell's list) and, after it, the lists of light indices. This writer makes one cell
/// that every fragment falls in. Buffer 2 holds 96-byte light records: the type at byte 16 (0 for a point light), the colour and inverse range at 32, the position at 48 and the
/// distance attenuation constants at 64 (the divisor is c0 + c1 d + c2 d squared, and the range term is (1 - (d / range) squared) squared). Read from programs 2061 and 12366.
/// </remarks>
public static class ClusterLights
{
    public const int MaxLights = 16, RecordSize = 96, CellBytes = (2 + MaxLights) * 4, RecordBytes = MaxLights * RecordSize;
    public const int TableBase = 0, CellWords = 2, ListBase = 2;

    public static uint[] Cells(int count)
    {
        uint[] words = new uint[2 + MaxLights];
        int n = Math.Min(count, MaxLights);
        words[0] = (uint)(n - 1);
        words[1] = 1;
        for (int i = 0; i < n; i++) words[ListBase + i] = (uint)i;
        return words;
    }

    public static float[] Records(IReadOnlyList<ClusterLight> lights)
    {
        float[] data = new float[MaxLights * RecordSize / 4];
        for (int i = 0; i < Math.Min(lights.Count, MaxLights); i++)
        {
            int at = i * RecordSize / 4;
            ClusterLight light = lights[i];
            data[at + 4] = BitConverter.Int32BitsToSingle(0);
            data[at + 8] = light.Color.X; data[at + 9] = light.Color.Y; data[at + 10] = light.Color.Z; data[at + 11] = 1f / light.Range;
            data[at + 12] = light.Position.X; data[at + 13] = light.Position.Y; data[at + 14] = light.Position.Z;
            data[at + 16] = 1f; data[at + 17] = 0f; data[at + 18] = 0f;
        }

        return data;
    }
}
