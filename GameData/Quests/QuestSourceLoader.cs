using System.Text;
using GdCli.GameData;
using GdCli.GameData.Arc;
using GdCli.GameData.Conversations;
using GdCli.GameData.Localization;
using GdCli.GameData.Lua;

namespace GdCli.GameData.Quests;

internal static class QuestSourceLoader
{
    public static QuestSourceCatalog Load(GameInstall install)
    {
        var localizedText = _loadLocalization(install);
        return new QuestSourceCatalog
        {
            Quests = _loadQuests(install, localizedText),
            Conversations = _loadConversations(install),
            LuaFunctions = _loadLuaFunctions(install)
        };
    }

    private static Dictionary<string, IReadOnlyList<string>> _loadLocalization(GameInstall install)
    {
        var result = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var source in install.Sources)
            _mergeLocalization(result, source.EnglishTagsPath);
        if (install.GameLanguage == "EN")
            return result;
        foreach (var source in install.Sources)
            _mergeLocalization(result, source.LocalizedTagsPath);
        return result;
    }

    private static void _mergeLocalization(
        Dictionary<string, IReadOnlyList<string>> destination,
        string? path)
    {
        if (path == null)
            return;
        foreach (var entry in PositionalLocalizationReader.Read(path))
            destination[entry.Key] = entry.Value;
    }

    private static Dictionary<string, QuestDefinition> _loadQuests(
        GameInstall install,
        Dictionary<string, IReadOnlyList<string>> localizedText)
    {
        var result = new Dictionary<string, QuestDefinition>(StringComparer.OrdinalIgnoreCase);
        var uniqueByName = _uniqueLocalizationByName(localizedText);
        foreach (var source in install.Sources)
        {
            if (source.QuestsPath == null)
                continue;
            using var archive = new ArcArchive(source.QuestsPath);
            foreach (var entry in archive.Entries.Where(entry =>
                         entry.Path.EndsWith(".qst", StringComparison.OrdinalIgnoreCase)))
            {
                var key = _resourceKey(entry.Path);
                if (!localizedText.TryGetValue(key, out var text))
                    uniqueByName.TryGetValue(_fileName(key), out text);
                using var stream = archive.OpenEntry(entry.Path);
                var quest = QuestReader.Read(stream, entry.Path, source.Name, text);
                result[quest.Path] = quest;
            }
        }
        return result;
    }

    private static Dictionary<string, IReadOnlyList<string>> _uniqueLocalizationByName(
        Dictionary<string, IReadOnlyList<string>> localizedText)
    {
        var result = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase);
        var duplicates = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in localizedText)
        {
            var name = _fileName(entry.Key);
            if (!result.TryAdd(name, entry.Value))
                duplicates.Add(name);
        }
        foreach (var duplicate in duplicates)
            result.Remove(duplicate);
        return result;
    }

    private static Dictionary<string, ConversationDefinition> _loadConversations(GameInstall install)
    {
        var result = new Dictionary<string, ConversationDefinition>(StringComparer.OrdinalIgnoreCase);
        foreach (var source in install.Sources)
        {
            if (source.ConversationsPath == null)
                continue;
            using var archive = new ArcArchive(source.ConversationsPath);
            foreach (var entry in archive.Entries.Where(entry =>
                         entry.Path.EndsWith(".cnv", StringComparison.OrdinalIgnoreCase)))
            {
                using var stream = archive.OpenEntry(entry.Path);
                var conversation = ConversationReader.Read(stream, entry.Path);
                result[conversation.Path] = conversation;
            }
        }
        return result;
    }

    private static Dictionary<string, LuaFunctionMetadata> _loadLuaFunctions(GameInstall install)
    {
        var result = new Dictionary<string, LuaFunctionMetadata>(StringComparer.OrdinalIgnoreCase);
        foreach (var source in install.Sources)
        {
            if (source.ScriptsPath == null)
                continue;
            using var archive = new ArcArchive(source.ScriptsPath);
            foreach (var entry in archive.Entries.Where(entry =>
                         entry.Path.EndsWith(".lua", StringComparison.OrdinalIgnoreCase)))
            {
                var script = Encoding.UTF8.GetString(archive.ReadEntry(entry.Path));
                foreach (var function in LuaQuestScanner.Scan(script))
                    result[function.Name] = function;
            }
        }
        return result;
    }

    private static string _resourceKey(string path)
    {
        var normalized = path.Replace('\\', '/');
        return normalized[..normalized.LastIndexOf('.')].ToLowerInvariant();
    }

    private static string _fileName(string path)
    {
        var separator = path.LastIndexOf('/');
        return separator < 0 ? path : path[(separator + 1)..];
    }
}
