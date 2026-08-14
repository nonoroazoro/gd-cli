using GdCli.GameData.Binary;
using GdCli.GameData.Scriptables;

namespace GdCli.GameData.Quests;

internal static class QuestReader
{
    private const int _minimumVersion = 9;
    private const int _maximumVersion = 9;

    public static QuestDefinition Read(
        Stream stream,
        string path,
        string source,
        IReadOnlyList<string>? localizedText = null)
    {
        using var reader = new GameBinaryReader(stream, true);
        if (!reader.ReadMagic("QST2"u8))
            throw new GameDataException($"Unsupported quest header: {path}");
        var version = reader.ReadInt32();
        if (version is < _minimumVersion or > _maximumVersion)
            throw new GameDataException($"Unsupported quest version in {path}: {version}");
        _ = reader.ReadUInt32();
        var uid = reader.ReadUInt32();
        var flags = reader.ReadUInt32();
        var taskCount = _readCount(reader, "quest task", path);
        var tasks = new List<QuestTask>(taskCount);
        for (var ordinal = 0; ordinal < taskCount; ordinal++)
            tasks.Add(_readTask(reader, ordinal, path));

        var embeddedText = _readLocalization(reader, false, path);
        var quest = new QuestDefinition
        {
            Path = _normalizeQuestPath(path),
            Source = source,
            Uid = uid,
            Flags = flags,
            Tasks = tasks
        };
        _applyText(quest, localizedText is { Count: > 0 } ? localizedText : embeddedText);
        return quest;
    }

    private static QuestTask _readTask(GameBinaryReader reader, int ordinal, string path)
    {
        var uid = reader.ReadUInt32();
        var flags = reader.ReadUInt32();
        var onAccept = _readEvents(reader, "onAccept", path);
        var objectives = _readObjectives(reader, path);
        var onComplete = _readEvents(reader, "onComplete", path);
        return new QuestTask
        {
            Ordinal = ordinal,
            Uid = uid,
            Flags = flags,
            IsBlocker = reader.ReadBoolean(),
            DontPropagate = reader.ReadBoolean(),
            OnAccept = onAccept,
            Objectives = objectives,
            OnComplete = onComplete
        };
    }

    private static List<QuestEvent> _readEvents(GameBinaryReader reader, string phase, string path)
    {
        var count = _readCount(reader, "quest event", path);
        var result = new List<QuestEvent>(count);
        for (var ordinal = 0; ordinal < count; ordinal++)
        {
            result.Add(new QuestEvent
            {
                Phase = phase,
                Ordinal = ordinal,
                Flags = reader.ReadUInt32(),
                Conditions = ScriptableReader.ReadConditions(reader),
                Actions = ScriptableReader.ReadActions(reader)
            });
        }
        return result;
    }

    private static List<QuestObjective> _readObjectives(GameBinaryReader reader, string path)
    {
        var count = _readCount(reader, "quest objective", path);
        var result = new List<QuestObjective>(count);
        for (var ordinal = 0; ordinal < count; ordinal++)
        {
            result.Add(new QuestObjective
            {
                Ordinal = ordinal,
                Uid = reader.ReadUInt32(),
                Flags = reader.ReadUInt32(),
                Conditions = ScriptableReader.ReadConditions(reader),
                Actions = ScriptableReader.ReadActions(reader)
            });
        }
        return result;
    }

    private static List<string> _readLocalization(GameBinaryReader reader, bool unicode, string path)
    {
        var localizationCount = _readCount(reader, "quest language", path);
        var result = new List<string>();
        for (var language = 0; language < localizationCount; language++)
        {
            _ = reader.ReadString();
            var count = _readCount(reader, "quest localization", path);
            var values = new List<string>(count);
            for (var index = 0; index < count; index++)
                values.Add(unicode ? reader.ReadUnicodeString() : reader.ReadString());
            if (result.Count == 0)
                result = values;
        }
        return result;
    }

    private static void _applyText(QuestDefinition quest, IReadOnlyList<string> text)
    {
        var index = 0;
        quest.Region = _next(text, ref index);
        quest.Name = _next(text, ref index);
        foreach (var task in quest.Tasks)
        {
            task.Name = _next(text, ref index);
            task.Description = _next(text, ref index);
            foreach (var entry in task.OnAccept)
                entry.Name = _next(text, ref index);
            foreach (var entry in task.Objectives)
                entry.Name = _next(text, ref index);
            foreach (var entry in task.OnComplete)
                entry.Name = _next(text, ref index);
        }
    }

    private static string _next(IReadOnlyList<string> text, ref int index)
    {
        return index < text.Count ? text[index++].Trim() : string.Empty;
    }

    private static int _readCount(GameBinaryReader reader, string subject, string path)
    {
        var count = reader.ReadInt32();
        if (count is < 0 or > 1_000_000)
            throw new GameDataException($"Invalid {subject} count in {path}: {count}");
        return count;
    }

    private static string _normalizeQuestPath(string path)
    {
        var normalized = path.Replace('\\', '/').ToLowerInvariant();
        return normalized.StartsWith("quests/", StringComparison.Ordinal) ? normalized : $"quests/{normalized}";
    }
}
