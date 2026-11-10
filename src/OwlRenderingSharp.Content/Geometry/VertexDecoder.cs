using System.Buffers.Binary;

namespace OwlRenderingSharp.Content.Geometry;

/// <summary>Decodes one stored vertex attribute into the four 32-bit words a shader input register holds.</summary>
/// <remarks>
/// Float and normalized kinds come out as floats, the integer kinds as the raw bits of the integer, which is how the game's shaders take indices and
/// weights. Components the format lacks are 0, and the fourth is 1 (as a float, or the integer 1 for an integer kind), the same defaults GL gives a
/// disabled attribute.
/// </remarks>
public static class VertexDecoder
{
    public static void Decode(VertexFormat format, ReadOnlySpan<byte> source, Span<uint> output)
    {
        bool integer = format.Kind is VertexValueKind.UInt or VertexValueKind.SInt;
        output[0] = output[1] = output[2] = 0;
        output[3] = integer ? 1u : BitConverter.SingleToUInt32Bits(1f);
        for (int i = 0; i < format.Components; i++)
            output[i] = format.Packed1010102 ? Packed(format.Kind, BinaryPrimitives.ReadUInt32LittleEndian(source), i) : Component(format, source, i);
        if (format.Packed1010102) output[3] = Packed(format.Kind, BinaryPrimitives.ReadUInt32LittleEndian(source), 3);
    }

    private static uint Component(VertexFormat format, ReadOnlySpan<byte> source, int index)
    {
        int bytes = format.ComponentBits / 8;
        ReadOnlySpan<byte> at = source[(index * bytes)..];
        bool signed = format.Kind is VertexValueKind.SNorm or VertexValueKind.SInt or VertexValueKind.SIntToFloat;
        if (format.Kind == VertexValueKind.Float)
            return bytes switch
            {
                2 => BitConverter.SingleToUInt32Bits((float)BinaryPrimitives.ReadHalfLittleEndian(at)),
                _ => BinaryPrimitives.ReadUInt32LittleEndian(at),
            };

        long value = bytes switch
        {
            1 => signed ? (sbyte)at[0] : at[0],
            2 => signed ? BinaryPrimitives.ReadInt16LittleEndian(at) : BinaryPrimitives.ReadUInt16LittleEndian(at),
            _ => signed ? BinaryPrimitives.ReadInt32LittleEndian(at) : BinaryPrimitives.ReadUInt32LittleEndian(at),
        };
        return Convert(format.Kind, value, format.ComponentBits);
    }

    // Ten-bit components with a two-bit alpha; for a signed-normalized word only the alpha stays unsigned.
    private static uint Packed(VertexValueKind kind, uint word, int index)
    {
        int bits = index == 3 ? 2 : 10;
        uint raw = word >> (index * 10) & (index == 3 ? 3u : 0x3ffu);
        bool signed = kind is VertexValueKind.SNorm or VertexValueKind.SInt && index != 3;
        long value = signed && (raw & 1u << bits - 1) != 0 ? (long)raw - (1L << bits) : raw;
        return Convert(index == 3 && kind == VertexValueKind.SNorm ? VertexValueKind.UNorm : kind, value, bits);
    }

    private static uint Convert(VertexValueKind kind, long value, int bits) => kind switch
    {
        VertexValueKind.UNorm => BitConverter.SingleToUInt32Bits((float)value / ((1L << bits) - 1)),
        VertexValueKind.SNorm => BitConverter.SingleToUInt32Bits(Math.Max(-1f, (float)value / ((1L << bits - 1) - 1))),
        VertexValueKind.UInt or VertexValueKind.SInt => unchecked((uint)value),
        VertexValueKind.UIntToFloat or VertexValueKind.SIntToFloat => BitConverter.SingleToUInt32Bits(value),
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };
}
