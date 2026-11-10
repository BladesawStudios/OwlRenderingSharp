using OwlRenderingSharp.Bench;

namespace OwlRenderingSharp.Gpu.Tests;

/// <summary>One headless context shared by the GPU tests; tests skip when the machine has none.</summary>
public sealed class GlFixture : IDisposable
{
    public HeadlessGl? Context { get; } = HeadlessGl.TryCreate();

    public HeadlessGl Require()
    {
        Skip.If(Context is null, $"No OpenGL 4.5 context: {HeadlessGl.UnavailableReason}");
        return Context!;
    }

    public void Dispose() => Context?.Dispose();
}

[CollectionDefinition(Name)]
public sealed class GlCollection : ICollectionFixture<GlFixture>
{
    public const string Name = "GL";
}
