using OwlRenderingSharp.Gpu;

namespace OwlRenderingSharp.Graph;

/// <summary>The game's own screen post chain, run from its shader archive: bloom, the white balance matrix, then the compose that applies the exposure and the tone curve.</summary>
/// <remarks>
/// The archive is the one the game ships its screen shaders in (added to the content as <see cref="Archive"/>). Bloom is this library's (<see cref="BloomPass"/>); <c>white_balance</c> turns a colour
/// temperature and tint into a matrix and applies it; <c>hdr_compose</c> multiplies by its colour matrix (the identity here), adds the bloom to the power the palette gives, multiplies by the exposure and applies the
/// filmic curve (the compose program whose options are the power form of bloom compose and tone mapping). The result is display-linear in sixteen-bit float, the size of the input, and is
/// owned by the caller. The exposure is the one value of the chain that is not read from the game's data.
/// </remarks>
public sealed class GamePostChain : IDisposable
{
    public const string Archive = "Agl";
    private const int ComposeBase = 384, ComposeWithBloomPower = 1920;

    private readonly RenderServices _services;
    private readonly ShaderScreenPass _balance, _compose, _composeWithBloom;
    private readonly BloomPass _bloom;

    public GamePostChain(RenderServices services)
    {
        _services = services;
        _balance = new ShaderScreenPass(services, Archive, "white_balance", 0);
        _compose = new ShaderScreenPass(services, Archive, "hdr_compose", ComposeBase);
        _composeWithBloom = new ShaderScreenPass(services, Archive, "hdr_compose", ComposeWithBloomPower);
        _bloom = new BloomPass(services.Device);
    }

    public GpuTexture Run(GpuTexture hdr, PostSettings settings)
    {
        GpuDevice device = _services.Device;
        using GpuTexture balanced = new(device, "post balanced", GpuFormat.Rgba16F, hdr.Width, hdr.Height);
        using (GpuFramebuffer target = new(device, "post balanced", [balanced]))
            _balance.Run(target, new Dictionary<string, uint> { ["cColor"] = hdr.Handle }, new Dictionary<string, byte[]> { ["WhiteBalanceParam"] = Floats(settings.WhiteBalanceTemperature, settings.WhiteBalanceTint, 0f, 0f) });

        GpuTexture result = new(device, "post composed", GpuFormat.Rgba16F, hdr.Width, hdr.Height);
        using GpuFramebuffer composed = new(device, "post composed", [result]);
        byte[] param = ComposeParams(settings);
        if (settings.BloomIntensity > 0f)
        {
            using GpuTexture bloom = _bloom.Run(balanced, settings);
            _composeWithBloom.Run(composed, new Dictionary<string, uint> { ["cHDRImage"] = balanced.Handle, ["cBloom"] = bloom.Handle }, new Dictionary<string, byte[]> { ["HdrComposeParam"] = param });
        }
        else
        {
            _compose.Run(composed, new Dictionary<string, uint> { ["cHDRImage"] = balanced.Handle }, new Dictionary<string, byte[]> { ["HdrComposeParam"] = param });
        }

        return result;
    }

    // Rows 0-2 are the colour matrix with its offset in w (the identity); row 3 holds the bloom power in y and the exposure in z.
    private static byte[] ComposeParams(PostSettings settings) => Floats(
        1, 0, 0, 0,
        0, 1, 0, 0,
        0, 0, 1, 0,
        0, settings.BloomPower, settings.Exposure, 0);

    private static byte[] Floats(params float[] values)
    {
        byte[] bytes = new byte[Math.Max(values.Length * sizeof(float), 144)];
        Buffer.BlockCopy(values, 0, bytes, 0, values.Length * sizeof(float));
        return bytes;
    }

    public void Dispose()
    {
        _bloom.Dispose();
        _composeWithBloom.Dispose();
        _compose.Dispose();
        _balance.Dispose();
    }
}
