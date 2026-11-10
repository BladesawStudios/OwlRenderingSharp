using System.Numerics;
using BfShaderSharp.Pack.Records;
using OwlRenderingSharp.Blocks;
using OwlRenderingSharp.Graph;

namespace OwlRenderingSharp.Games.Oot;

/// <summary>Fills the uniform blocks of the game's model shading: the camera in the context, the instance position in the shape block and the bind-pose skeleton palette.</summary>
/// <remarks>
/// The context, shape and skeleton blocks are laid out and filled; the material block comes from the pack; the environment and scene material blocks are frame-level and
/// stay zero until their fields are worked out. The palette is indexed by the skeleton's matrix number. In the bind pose a smooth matrix is the identity (its world matrix times its inverse bind) and a rigid
/// matrix, one past the smooth ones, is its bone's world matrix. The shape block carries only the translation of the world matrix.
/// </remarks>
public sealed class OotBlockProfile(OotBlockLayouts layouts) : IBlockProfile
{
    public OotBlockProfile() : this(new OotBlockLayouts())
    {
    }

    public BlockScope Scope(string block) => block switch
    {
        "gsys_material" => BlockScope.Material,
        OotBlockLayouts.Shape or OotBlockLayouts.Skeleton => BlockScope.Shape,
        OotBlockLayouts.Context or "gsys_environment" or "gsys_scene_material" => BlockScope.Frame,
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
            case OotBlockLayouts.Shape when shape is not null:
                writer.Set("Position", new Vector4(shape.World.Translation, 1f));
                break;
            case OotBlockLayouts.Skeleton when shape is not null:
                FillPalette(writer, shape);
                break;
        }
    }

    // The shader indexes the palette by the skeleton's matrix number, not by the shape's own list of skin bones.
    private static int PaletteSize(ShapeInfo shape) => shape.Skeleton.MatrixToBone.Count;

    private static void FillPalette(BlockWriter writer, ShapeInfo shape)
    {
        SkeletonRecord skeleton = shape.Skeleton;
        Matrix4x4[] worlds = BindPose.Worlds(skeleton);
        for (int i = 0; i < skeleton.MatrixToBone.Count; i++)
        {
            int bone = skeleton.MatrixToBone[i];
            bool rigid = i >= skeleton.InverseMatrices.Count && bone >= 0 && bone < worlds.Length;
            writer.Set(OotBlockLayouts.Bone(i), rigid ? worlds[bone] : Matrix4x4.Identity);
        }
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
        writer.Set("Resolution", new Vector4(frame.Width, frame.Height, 1f / frame.Width, 1f / frame.Height));
    }
}
