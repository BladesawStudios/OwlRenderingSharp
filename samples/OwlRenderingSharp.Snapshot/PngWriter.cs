using System.Buffers.Binary;
using System.IO.Compression;

namespace OwlRenderingSharp.Snapshot;

/// <summary>Writes an 8-bit RGBA image as a PNG, top row first.</summary>
public static class PngWriter
{
    private static readonly uint[] CrcTable = BuildTable();

    public static void Write(string path, int width, int height, ReadOnlySpan<byte> rgba, bool keepAlpha = false)
    {
        using MemoryStream raw = new();
        using (ZLibStream zlib = new(raw, CompressionLevel.Fastest, leaveOpen: true))
        {
            for (int y = 0; y < height; y++)
            {
                zlib.WriteByte(0);
                byte[] row = rgba.Slice(y * width * 4, width * 4).ToArray();
                if (!keepAlpha) for (int x = 3; x < row.Length; x += 4) row[x] = 255;
                zlib.Write(row);
            }
        }

        using FileStream file = File.Create(path);
        file.Write([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]);
        byte[] header = new byte[13];
        BinaryPrimitives.WriteInt32BigEndian(header, width);
        BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(4), height);
        header[8] = 8;
        header[9] = 6;
        Chunk(file, "IHDR", header);
        Chunk(file, "IDAT", raw.ToArray());
        Chunk(file, "IEND", []);
    }

    private static void Chunk(Stream stream, string type, byte[] data)
    {
        byte[] length = new byte[4];
        BinaryPrimitives.WriteInt32BigEndian(length, data.Length);
        stream.Write(length);
        byte[] body = [.. System.Text.Encoding.ASCII.GetBytes(type), .. data];
        stream.Write(body);
        byte[] crc = new byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(crc, Crc(body));
        stream.Write(crc);
    }

    private static uint Crc(byte[] bytes)
    {
        uint crc = 0xFFFFFFFF;
        foreach (byte b in bytes) crc = CrcTable[(crc ^ b) & 0xFF] ^ (crc >> 8);
        return crc ^ 0xFFFFFFFF;
    }

    private static uint[] BuildTable()
    {
        uint[] table = new uint[256];
        for (uint n = 0; n < 256; n++)
        {
            uint c = n;
            for (int k = 0; k < 8; k++) c = (c & 1) != 0 ? 0xEDB88320 ^ (c >> 1) : c >> 1;
            table[n] = c;
        }

        return table;
    }
}
