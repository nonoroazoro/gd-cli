using GdCli.GameData.Binary;
using GdCli.GameData.Scriptables;

namespace GdCli.GameData.Conversations;

internal static class ConversationReader
{
    private static readonly string[] _stepTypes =
    [
        "node", "speech", "continue", "accept", "decline", "generic", "end", "link", "random"
    ];

    public static ConversationDefinition Read(Stream stream, string path)
    {
        using var reader = new GameBinaryReader(stream, true);
        if (!reader.ReadMagic("CNV "u8))
            throw new GameDataException($"Unsupported conversation header: {path}");
        var version = reader.ReadInt32();
        if (version is < 2 or > 6)
            throw new GameDataException($"Unsupported conversation version in {path}: {version}");
        if (version >= 4)
            _ = reader.ReadUInt32();
        _ = reader.ReadUInt32();
        var ordinal = 0;
        ConversationStep root;
        try
        {
            root = _readStep(reader, version, ref ordinal, path);
            _skipLocalization(reader, version < 5, path);
        }
        catch (GameDataException exception)
        {
            throw new GameDataException($"Failed to parse conversation {path} at {reader.Position}: {exception.Message}", exception);
        }
        return new ConversationDefinition
        {
            Path = _normalizePath(path),
            Root = root
        };
    }

    public static IEnumerable<ConversationStep> Traverse(ConversationStep root)
    {
        var stack = new Stack<ConversationStep>();
        stack.Push(root);
        while (stack.Count > 0)
        {
            var current = stack.Pop();
            yield return current;
            for (var index = current.Children.Count - 1; index >= 0; index--)
                stack.Push(current.Children[index]);
        }
    }

    private static ConversationStep _readStep(GameBinaryReader reader, int version, ref int ordinal, string path)
    {
        var childCount = _readCount(reader, "conversation child", path);
        var typeValue = reader.ReadInt32();
        if (typeValue < 0 || typeValue >= _stepTypes.Length)
            throw new GameDataException($"Unsupported conversation step type in {path}: {typeValue}");
        var flags = reader.ReadUInt32();
        int? linkId = typeValue == 7 ? reader.ReadInt32() : null;
        if (version >= 6)
            _ = reader.ReadString();
        var conditions = ScriptableReader.ReadConditions(reader);
        IReadOnlyList<ScriptableValue> actions;
        try
        {
            actions = ScriptableReader.ReadActions(reader);
        }
        catch (GameDataException exception)
        {
            var conditionKinds = string.Join(',', conditions.Values.Select(value => value.Kind));
            throw new GameDataException(
                $"Step {ordinal} ({_stepTypes[typeValue]}) conditions [{conditionKinds}]: {exception.Message}",
                exception);
        }
        var currentOrdinal = ordinal++;
        var children = new List<ConversationStep>(childCount);
        for (var index = 0; index < childCount; index++)
            children.Add(_readStep(reader, version, ref ordinal, path));
        return new ConversationStep
        {
            Ordinal = currentOrdinal,
            Type = _stepTypes[typeValue],
            Flags = flags,
            LinkId = linkId,
            Conditions = conditions,
            Actions = actions,
            Children = children
        };
    }

    private static void _skipLocalization(GameBinaryReader reader, bool unicode, string path)
    {
        var localizationCount = _readCount(reader, "conversation language", path);
        for (var language = 0; language < localizationCount; language++)
        {
            _ = reader.ReadString();
            var count = _readCount(reader, "conversation localization", path);
            for (var index = 0; index < count; index++)
                _ = unicode ? reader.ReadUnicodeString() : reader.ReadString();
        }
    }

    private static int _readCount(GameBinaryReader reader, string subject, string path)
    {
        var count = reader.ReadInt32();
        if (count is < 0 or > 1_000_000)
            throw new GameDataException($"Invalid {subject} count in {path}: {count}");
        return count;
    }

    private static string _normalizePath(string path)
    {
        var normalized = path.Replace('\\', '/').ToLowerInvariant();
        return normalized.StartsWith("conversations/", StringComparison.Ordinal)
            ? normalized
            : $"conversations/{normalized}";
    }
}
