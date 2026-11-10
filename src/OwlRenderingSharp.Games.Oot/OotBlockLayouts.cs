using System.Reflection;
using OwlRenderingSharp.Blocks;

namespace OwlRenderingSharp.Games.Oot;

/// <summary>The uniform block layouts of the game, read from the layout files embedded in this assembly and the skeleton layout built per bone count.</summary>
/// <remarks>
/// Every field in a layout file names the evidence for its place. A block with no file is not laid out and stays zero. The skeleton block
/// is one 3x4 matrix of 48 bytes per bone of the shape from byte 0, so its layout depends on the shape and is built on demand and kept.
/// </remarks>
public sealed class OotBlockLayouts
{
    public const string Context = "gsys_context", Environment = "gsys_environment", Shape = "gsys_shape", Skeleton = "gsys_skeleton";
    private const int BoneBytes = 48;
    private readonly Dictionary<string, BlockLayout> _files = [];
    private readonly Dictionary<int, BlockLayout> _skeletons = [];

    public OotBlockLayouts()
    {
        Assembly assembly = typeof(OotBlockLayouts).Assembly;
        foreach (string resource in assembly.GetManifestResourceNames().Where(n => n.EndsWith(".json", StringComparison.Ordinal)))
        {
            using StreamReader reader = new(assembly.GetManifestResourceStream(resource)!);
            BlockLayout layout = BlockLayoutFile.Parse(reader.ReadToEnd());
            _files[layout.Name] = layout;
        }
    }

    public BlockLayout? Find(string block) => _files.GetValueOrDefault(block);

    public BlockLayout SkeletonFor(int boneCount)
    {
        int count = Math.Max(1, boneCount);
        if (!_skeletons.TryGetValue(count, out BlockLayout? layout))
        {
            List<BlockField> fields = [.. Enumerable.Range(0, count).Select(i =>
                new BlockField(Bone(i), i * BoneBytes, BlockFieldType.Matrix3x4, "The vertex stage of program 20052 reads the rows of a bone at 48 * the bone index, 16 bytes apart; program 6183 reads rows 1 and 2 at +16."))];
            _skeletons[count] = layout = new BlockLayout(Skeleton, count * BoneBytes, fields);
        }

        return layout;
    }

    public static string Bone(int index) => $"Bone{index}";
}
