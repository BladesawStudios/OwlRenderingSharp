namespace OwlRenderingSharp.Graph;

/// <summary>How often a uniform block's contents change, which decides who fills it and who binds it.</summary>
public enum BlockScope
{
    /// <summary>Not known to the profile; the block stays zero.</summary>
    Unknown,

    /// <summary>Camera, lights and environment: one buffer per frame, bound once for every shape.</summary>
    Frame,

    /// <summary>Per drawn shape: its transform, skeleton palette and the like.</summary>
    Shape,

    /// <summary>The material's own parameters, prepared in the pack.</summary>
    Material,
}
