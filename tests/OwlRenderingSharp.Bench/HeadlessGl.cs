using System.Collections.Concurrent;
using OwlRenderingSharp.Gpu;
using Silk.NET.Core.Contexts;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;

namespace OwlRenderingSharp.Bench;

/// <summary>An OpenGL 4.5 core context in a hidden window, owned by a thread of its own that tests hand work to.</summary>
/// <remarks>
/// A GL context belongs to one thread, and a test runner moves tests between threads, so the context thread is the only one that ever touches GL:
/// <see cref="Run{T}"/> queues a function onto it and waits for the result, rethrowing what it throws. <see cref="TryCreate"/> returns null
/// with the reason when the machine has no usable GL, so a test can skip instead of failing.
/// </remarks>
public sealed class HeadlessGl : IDisposable
{
    private readonly BlockingCollection<Action> _work = [];
    private readonly Thread _thread;
    private GpuDevice? _device;
    private IWindow? _window;

    private HeadlessGl()
    {
        _thread = new Thread(Loop) { IsBackground = true, Name = "Owl headless GL" };
    }

    public static string? UnavailableReason { get; private set; }

    public static HeadlessGl? TryCreate()
    {
        HeadlessGl context = new();
        TaskCompletionSource<string?> ready = new();
        context._work.Add(() => ready.SetResult(context.Open()));
        context._thread.Start();
        string? failure = ready.Task.GetAwaiter().GetResult();
        if (failure is null) return context;
        UnavailableReason = failure;
        context.Dispose();
        return null;
    }

    public T Run<T>(Func<GpuDevice, T> function)
    {
        TaskCompletionSource<T> done = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _work.Add(() =>
        {
            try
            {
                done.SetResult(function(_device!));
            }
            catch (Exception e)
            {
                done.SetException(e);
            }
        });
        return done.Task.GetAwaiter().GetResult();
    }

    public void Run(Action<GpuDevice> action) => Run<object?>(device =>
    {
        action(device);
        return null;
    });

    public void Dispose()
    {
        _work.CompleteAdding();
        if (_thread.IsAlive) _thread.Join();
    }

    private void Loop()
    {
        foreach (Action action in _work.GetConsumingEnumerable()) action();
        _window?.Dispose();
    }

    // Runs on the context thread; creates the window and the device, and returns why it could not.
    private string? Open()
    {
        try
        {
            Window.PrioritizeGlfw();
            WindowOptions options = WindowOptions.Default with
            {
                IsVisible = false,
                Size = new Silk.NET.Maths.Vector2D<int>(64, 64),
                API = new GraphicsAPI(ContextAPI.OpenGL, ContextProfile.Core, ContextFlags.ForwardCompatible | ContextFlags.Debug, new APIVersion(4, 5)),
            };
            IWindow window = Window.Create(options);
            window.Initialize();
            _window = window;
            _device = new GpuDevice(GL.GetApi((IGLContextSource)window));
            return null;
        }
        catch (Exception e)
        {
            return e.Message;
        }
    }
}
