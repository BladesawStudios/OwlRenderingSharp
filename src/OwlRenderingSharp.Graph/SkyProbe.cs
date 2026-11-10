using System.Numerics;
using OwlRenderingSharp.Gpu;

namespace OwlRenderingSharp.Graph;

/// <summary>A stand-in reflection cube built from a sky colour above and a ground colour below, offered to the frame as <c>gsys_cube_map</c>.</summary>
/// <remarks>
/// The game reflects a baked probe of the place; with none, the hemisphere light stands in, blended the way the game's own <c>hemi_to_env</c> program blends it: the weight of the ground is
/// <c>clamp(0.5 - 0.5 * sign(y) * |y|^0.33)</c> of the direction's height, so the horizon is an even mix and the sky and ground fill most of their halves. The faces are linear
/// float, so a sky brighter than one is kept, and have a full mip chain: the shading programs read a blurred level by roughness and the last level for ambient light, at the level count the
/// environment block gives (<see cref="MipLevels"/> less one). It is used from the GL thread only and disposes its texture and sampler.
/// </remarks>
public sealed class SkyProbe : IDisposable
{
    public const string SamplerName = "gsys_cube_map";
    public const int FaceSize = 16;

    /// <summary>The number of mip levels of the cube: the environment block's reflection level is one less.</summary>
    public static readonly int MipLevels = (int)Math.Log2(FaceSize) + 1;

    private readonly GpuCubeTexture _cube;
    private readonly GpuSampler _sampler;
    private readonly FrameTexture _texture;

    public SkyProbe(RenderServices services, Vector3 sky, Vector3 ground)
    {
        _cube = new GpuCubeTexture(services.Device, "sky probe", FaceSize, Faces(sky, ground, FaceSize));
        _sampler = new GpuSampler(services.Device, "sky probe", SamplerState.LinearClamp with { Min = TextureFilter.LinearMipLinear });
        _texture = new FrameTexture(_cube.Handle, _sampler.Handle);
    }

    public IReadOnlyDictionary<string, FrameTexture> Textures => new Dictionary<string, FrameTexture> { [SamplerName] = _texture };

    public void Dispose()
    {
        _sampler.Dispose();
        _cube.Dispose();
    }

    /// <summary>The colour looking along a direction: the sky and the ground blended by the direction's height.</summary>
    public static Vector3 Blend(Vector3 sky, Vector3 ground, Vector3 direction)
    {
        float y = Vector3.Normalize(direction).Y;
        float weight = Math.Clamp(0.5f - 0.5f * MathF.Sign(y) * MathF.Pow(MathF.Abs(y), 0.33f), 0f, 1f);
        return Vector3.Lerp(sky, ground, weight);
    }

    /// <summary>The six faces (+X, -X, +Y, -Y, +Z, -Z) of <paramref name="size"/> by <paramref name="size"/> texels, four floats each, top row first, in OpenGL's cube face orientation.</summary>
    public static float[][] Faces(Vector3 sky, Vector3 ground, int size)
    {
        float[][] faces = new float[6][];
        for (int face = 0; face < 6; face++)
        {
            faces[face] = new float[size * size * 4];
            for (int row = 0; row < size; row++)
                for (int column = 0; column < size; column++)
                {
                    float s = (column + 0.5f) / size * 2f - 1f, t = (row + 0.5f) / size * 2f - 1f;
                    Vector3 color = Blend(sky, ground, Direction(face, s, t));
                    int at = (row * size + column) * 4;
                    faces[face][at] = color.X;
                    faces[face][at + 1] = color.Y;
                    faces[face][at + 2] = color.Z;
                    faces[face][at + 3] = 1f;
                }
        }

        return faces;
    }

    private static Vector3 Direction(int face, float s, float t) => face switch
    {
        0 => new Vector3(1f, -t, -s),
        1 => new Vector3(-1f, -t, s),
        2 => new Vector3(s, 1f, t),
        3 => new Vector3(s, -1f, -t),
        4 => new Vector3(s, -t, 1f),
        _ => new Vector3(-s, -t, -1f),
    };
}
