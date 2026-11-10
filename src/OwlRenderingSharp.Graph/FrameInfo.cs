namespace OwlRenderingSharp.Graph;

/// <summary>What the frame-level blocks are filled from: the camera and the size of the target being drawn to.</summary>
public sealed record FrameInfo(Camera Camera, int Width, int Height)
{
    public float Aspect => (float)Width / Math.Max(1, Height);
}
