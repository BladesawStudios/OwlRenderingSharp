using System.Buffers.Binary;

namespace OwlRenderingSharp.Graph;

/// <summary>The bytes of the <c>_support_buffer</c> block the decompiled programs read for their own bookkeeping.</summary>
/// <remarks>
/// The block is std140: the alpha test word at 0, eight BGRA flags from 16, the inverse viewport at 144 and the viewport size at 160, the fragment scale count at
/// 176 and the render scale array from 192, one float per 16 bytes. The render scale must be one or the programs scale their coordinates by zero. The inverse
/// viewport is two over the size, negative in y, as the programs convert fragment coordinates with it.
/// </remarks>
public static class SupportBufferData
{
    public const int Size = 1392;
    private const int InverseViewport = 144, ViewportSize = 160, ScaleCount = 176, RenderScale = 192, RenderScaleCount = 73;

    public static byte[] Create(int width, int height)
    {
        byte[] bytes = new byte[Size];
        WriteFloats(bytes, InverseViewport, 2f / width, -2f / height, 1f, 1f);
        WriteFloats(bytes, ViewportSize, width, height, 0f, 0f);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(ScaleCount), 1);
        for (int i = 0; i < RenderScaleCount; i++) WriteFloats(bytes, RenderScale + i * 16, 1f);
        return bytes;
    }

    private static void WriteFloats(byte[] bytes, int at, params float[] values)
    {
        for (int i = 0; i < values.Length; i++) BinaryPrimitives.WriteSingleLittleEndian(bytes.AsSpan(at + i * 4), values[i]);
    }
}
