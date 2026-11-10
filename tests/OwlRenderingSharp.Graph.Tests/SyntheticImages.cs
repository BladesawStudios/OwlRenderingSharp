using OwlRenderingSharp.Gpu;

namespace OwlRenderingSharp.Graph.Tests;

/// <summary>Small images for the screen effect tests: a flat depth image, and a lit image that is black with one bright texel.</summary>
internal static class SyntheticImages
{
    public const int Size = 32;
    public const float Near = 0.1f, Far = 100f;

    public static Camera Camera => new() { NearPlane = Near, FarPlane = Far };

    public static GpuTexture Depth(GpuDevice device, float distance)
    {
        GpuTexture depth = new(device, "depth", GpuFormat.Depth32F, Size, Size);
        float stored = distance >= Far ? 1f : (1f - Near / distance) / (1f - Near / Far);
        depth.Upload<float>(Enumerable.Repeat(stored, Size * Size).ToArray());
        return depth;
    }

    public static GpuTexture Lit(GpuDevice device, bool dot)
    {
        GpuTexture lit = new(device, "lit", GpuFormat.Rgba32F, Size, Size);
        float[] pixels = new float[Size * Size * 4];
        for (int i = 0; i < Size * Size; i++)
        {
            bool centre = i == (Size / 2) * Size + Size / 2;
            float value = dot ? (centre ? 1f : 0f) : 1f;
            pixels[i * 4] = value;
            pixels[i * 4 + 1] = value;
            pixels[i * 4 + 2] = value;
            pixels[i * 4 + 3] = 1f;
        }

        lit.Upload<float>(pixels);
        return lit;
    }

    // The red channel at a texel of a float image.
    public static float Red(GpuTexture image, int x, int y) => image.DownloadAsFloats()[(y * Size + x) * 4];
}
