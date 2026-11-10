using System.Numerics;
using BfShaderSharp.Pack.Records;
using OwlRenderingSharp.Blocks;
using OwlRenderingSharp.Graph;

namespace OwlRenderingSharp.Games.Oot;

/// <summary>Fills the uniform blocks of the game's model shading: the camera in the context, the instance position in the shape block and the bind-pose skeleton palette.</summary>
/// <remarks>
/// The context, shape and skeleton blocks are laid out and filled; the material block comes from the pack; the environment block is frame-level and carries the lights of its
/// <see cref="Environment"/>; the scene material block stays zero until its fields are worked out. The palette is indexed by the skeleton's matrix number. In the bind pose a smooth matrix is the identity (its world matrix times its inverse bind) and a rigid
/// matrix, one past the smooth ones, is its bone's world matrix. The shape block carries only the translation of the world matrix.
/// </remarks>
public sealed class OotBlockProfile(OotBlockLayouts layouts, OotEnvironment environment) : IBlockProfile
{
    public OotBlockProfile() : this(new OotBlockLayouts(), OotEnvironment.Neutral)
    {
    }

    private const int ShadowCascadeCount = 3;

    // The lighting the environment block is filled from; the host changes it as the place or the time of day does.
    public OotEnvironment Environment { get; set; } = environment;

    // A multiplier on the sun's colour and the key and rim lights built from it; 1 is the palette's own level.
    public float SunScale { get; set; } = 1f;

    // How wet the scene is, 0 to 1: the environment half of the wetness control (see WetnessControl for the material half).
    public float Wetness { get; set; }

    public BlockScope Scope(string block) => block switch
    {
        "gsys_material" => BlockScope.Material,
        OotBlockLayouts.Shape or OotBlockLayouts.Skeleton => BlockScope.Shape,
        OotBlockLayouts.Context or OotBlockLayouts.Environment or OotBlockLayouts.SceneMaterial => BlockScope.Frame,
        _ => BlockScope.Unknown,
    };

    public BlockLayout? Layout(string block, ShapeInfo? shape) =>
        block == OotBlockLayouts.Skeleton && shape is not null ? layouts.SkeletonFor(PaletteSize(shape)) : layouts.Find(block);

    public void Fill(string block, BlockWriter writer, FrameInfo frame, ShapeInfo? shape)
    {
        switch (block)
        {
            case OotBlockLayouts.Context:
                FillContext(writer, frame);
                break;
            case OotBlockLayouts.Environment:
                OotLights.Fill(writer, Environment, SunScale);
                writer.Set("WetnessTerms", new Vector4(0f, 0f, WetnessControl.Clamp(Wetness), 0f));
                FillVolumes(writer, frame);
                break;
            case OotBlockLayouts.SceneMaterial:
                writer.Set("AmbientBlend", new Vector4(0f, 1f, 0f, 0f)).Set("LightScale", new Vector4(1f, 1f, 0f, 0f));
                break;
            case OotBlockLayouts.Shape when shape is not null:
                FillShape(writer, shape);
                break;
            case OotBlockLayouts.Skeleton when shape is not null:
                FillPalette(writer, shape);
                break;
        }
    }

    // The fog volumes' addressing: the distance range the slice is chosen in, and unit scales onto the screen. The volumes themselves are empty (no fog), so these only keep the lookups well defined.
    private static void FillVolumes(BlockWriter writer, FrameInfo frame)
    {
        writer.Set("FroxelNearFar", new Vector4(frame.Camera.NearPlane, frame.Camera.FarPlane, 0f, 0f));
        writer.Set("FroxelScale", new Vector4(1f, 1f, 1f, 0f));
    }

    // The entry of the light-probe table a shape reads: entry 0 is where the programs that walk the clustered lights read their header, so the probe sits at 1.
    public const int LightProbeEntry = 1;

    private static void FillShape(BlockWriter writer, ShapeInfo shape)
    {
        Matrix4x4[] worlds = BindPose.Worlds(shape.Skeleton);
        writer.Set("Model", shape.BoneIndex >= 0 && shape.BoneIndex < worlds.Length ? worlds[shape.BoneIndex] : Matrix4x4.Identity);
        writer.Set("LightIndex", new Vector4(BitConverter.Int32BitsToSingle(LightProbeEntry), 0f, 0f, 0f));
        writer.Set("Position", new Vector4(shape.World.Translation, 1f));
        writer.Set("HeightFadeBottom", Vector4.Zero).Set("HeightFadeTop", Vector4.One).Set("HeightFadeOffset", Vector4.Zero).Set("HeightFadeBase", Vector4.Zero);
    }

    // The shader indexes the palette by the skeleton's matrix number, not by the shape's own list of skin bones.
    private static int PaletteSize(ShapeInfo shape) => Math.Max(1, shape.Skeleton.MatrixToBone.Count);

    private static void FillPalette(BlockWriter writer, ShapeInfo shape)
    {
        SkeletonRecord skeleton = shape.Skeleton;
        Matrix4x4[] worlds = BindPose.Worlds(skeleton);
        if (skeleton.MatrixToBone.Count == 0) writer.Set(OotBlockLayouts.Bone(0), shape.BoneIndex >= 0 && shape.BoneIndex < worlds.Length ? worlds[shape.BoneIndex] : Matrix4x4.Identity);
        for (int i = 0; i < skeleton.MatrixToBone.Count; i++)
        {
            int bone = skeleton.MatrixToBone[i];
            bool rigid = i >= skeleton.InverseMatrices.Count && bone >= 0 && bone < worlds.Length;
            writer.Set(OotBlockLayouts.Bone(i), rigid ? worlds[bone] : Matrix4x4.Identity);
        }
    }

    // One cell holds every light; see ClusterLights for the buffers it indexes.
    private static void FillClusters(BlockWriter writer)
    {
        writer.Set("ClusterCountX", 1).Set("ClusterCountY", 1).Set("ClusterCountZ", 1).Set("ClusterStrideZ", 1);
        writer.Set("ClusterTableBase", ClusterLights.TableBase).Set("ClusterCellWords", ClusterLights.CellWords).Set("ClusterListBase", ClusterLights.ListBase);
        writer.Set("ClusterSliceScale", 1f).Set("ClusterDepthScale", 1f);
    }

    private static void FillContext(BlockWriter writer, FrameInfo frame)
    {
        Camera camera = frame.Camera;
        Matrix4x4 view = camera.ViewMatrix(), projection = camera.ProjectionMatrix(frame.Aspect);
        Matrix4x4.Invert(view, out Matrix4x4 inverseView);
        float near = camera.NearPlane, far = camera.FarPlane, aspect = frame.Aspect, tanHalf = MathF.Tan(camera.FovRadians / 2f);
        writer.Set("View", view).Set("ViewProjection", view * projection).Set("Projection", projection).Set("InverseView", inverseView);
        writer.Set("NearFar", new Vector4(near, far, near / far, 1f - near / far));
        writer.Set("DepthScale", new Vector4(1f / (far - near), near / (far - near), aspect, 1f / aspect));
        writer.Set("FarMinusNear", new Vector4(far - near, 1f / (1f - near / far), 0f, 0f));
        writer.Set("FieldOfView", new Vector4(aspect * tanHalf, tanHalf, camera.FovRadians, 0f));
        writer.Set("ProjectionA", ScreenRows(projection)).Set("ProjectionB", ScreenRows(projection)).Set("ShadowScale", new Vector4(1f, 1f, 0f, 0f)).Set("ScreenFade", Vector4.Zero);
        FillClusters(writer);
        writer.Set("Resolution", new Vector4(frame.Width, frame.Height, 1f / frame.Width, 1f / frame.Height));
        FillShadows(writer, frame, inverseView);
    }

    // Each cascade takes the view-space position the shader rebuilds, so its matrix is the inverse view followed by the cascade's world-to-shadow-texture matrix. A frame with fewer cascades
    // than the three the context holds repeats its last; a frame with none writes an identity that reads the neutral (unshadowed) map.
    private static void FillShadows(BlockWriter writer, FrameInfo frame, Matrix4x4 inverseView)
    {
        IReadOnlyList<ShadowCascade> cascades = frame.Cascades is { Count: > 0 } ? frame.Cascades : [];
        Vector4 splits = new(float.MaxValue);
        for (int i = 0; i < ShadowCascadeCount; i++)
        {
            ShadowCascade? cascade = cascades.Count == 0 ? null : cascades[Math.Min(i, cascades.Count - 1)];
            writer.Set($"ShadowCascade{i}", cascade is null ? Matrix4x4.Identity : inverseView * cascade.TextureMatrix);
            if (cascade is not null && i < cascades.Count - 1) splits = i switch { 0 => splits with { X = cascade.Split }, 1 => splits with { Y = cascade.Split }, _ => splits with { Z = cascade.Split } };
        }

        writer.Set("ShadowSplits", splits);
    }

    // The x, y and w rows of a projection in the form a 3x4 field takes: its third written row is the w row, which the matrix's columns 1, 2 and 4 supply.
    private static Matrix4x4 ScreenRows(Matrix4x4 m) => new(
        m.M11, m.M12, m.M14, m.M14,
        m.M21, m.M22, m.M24, m.M24,
        m.M31, m.M32, m.M34, m.M34,
        m.M41, m.M42, m.M44, m.M44);
}
