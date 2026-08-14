using System.Globalization;
using System.Text.RegularExpressions;

namespace GdCli.GameData.Lua;

internal static partial class LuaQuestScanner
{
    public static IReadOnlyList<LuaFunctionMetadata> Scan(string source)
    {
        var tables = _readStateTables(source);
        var matches = _functionRegex().Matches(source);
        var globalConstants = _readConstants(matches.Count == 0 ? source : source[..matches[0].Index]);
        var result = new List<LuaFunctionMetadata>(matches.Count);
        for (var index = 0; index < matches.Count; index++)
        {
            var match = matches[index];
            var end = index + 1 < matches.Count ? matches[index + 1].Index : source.Length;
            var body = source[match.Index..end];
            var constants = new Dictionary<string, uint>(globalConstants, StringComparer.Ordinal);
            foreach (var constant in _readConstants(body))
                constants[constant.Key] = constant.Value;
            var spawnedRecords = _readSpawnedRecords(body, tables);
            var questGrants = _readQuestGrants(body, constants);
            if (spawnedRecords.Length == 0 && questGrants.Count == 0)
                continue;
            result.Add(new LuaFunctionMetadata
            {
                Name = match.Groups["name"].Value,
                SpawnedRecordIds = spawnedRecords,
                QuestGrants = questGrants
            });
        }
        return result;
    }

    private static Dictionary<string, uint> _readConstants(string source)
    {
        var result = new Dictionary<string, uint>(StringComparer.Ordinal);
        foreach (Match match in _constantRegex().Matches(source))
        {
            if (_tryParseLiteral(match.Groups["value"].Value, out var value))
                result[match.Groups["name"].Value] = value;
        }
        return result;
    }

    private static Dictionary<string, IReadOnlyList<string>> _readStateTables(string source)
    {
        var result = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);
        foreach (Match declaration in _stateTableRegex().Matches(source))
        {
            var name = declaration.Groups["name"].Value;
            var endMatch = _functionRegex().Match(source, declaration.Index + declaration.Length);
            var end = endMatch.Success ? endMatch.Index : source.Length;
            var body = source[declaration.Index..end];
            var records = _dbrRegex().Matches(body)
                .Select(match => _normalizeRecord(match.Groups["record"].Value))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
            result[name] = records;
        }
        return result;
    }

    private static string[] _readSpawnedRecords(
        string body,
        Dictionary<string, IReadOnlyList<string>> tables)
    {
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (Match match in _objectSwapRegex().Matches(body))
        {
            if (!tables.TryGetValue(match.Groups["table"].Value, out var records))
                continue;
            foreach (var record in records)
                result.Add(record);
        }
        return result.Order(StringComparer.OrdinalIgnoreCase).ToArray();
    }

    private static List<LuaQuestGrant> _readQuestGrants(
        string body,
        Dictionary<string, uint> constants)
    {
        var result = new List<LuaQuestGrant>();
        foreach (Match match in _grantQuestRegex().Matches(body))
        {
            if (!_resolveNumber(match.Groups["quest"].Value, constants, out var quest) ||
                !_resolveNumber(match.Groups["task"].Value, constants, out var task))
                continue;
            result.Add(new LuaQuestGrant { QuestUid = quest, TaskUid = task });
        }
        return result;
    }

    private static bool _resolveNumber(string text, Dictionary<string, uint> constants, out uint value)
    {
        var token = text.Trim();
        return _tryParseLiteral(token, out value) ||
               constants.TryGetValue(token, out value);
    }

    private static bool _tryParseLiteral(string token, out uint value)
    {
        return token.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
            ? uint.TryParse(
                token.AsSpan(2),
                NumberStyles.AllowHexSpecifier,
                CultureInfo.InvariantCulture,
                out value)
            : uint.TryParse(token, NumberStyles.None, CultureInfo.InvariantCulture, out value);
    }

    private static string _normalizeRecord(string value) => value.Replace('\\', '/').ToLowerInvariant();

    [GeneratedRegex(@"\blocal\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)\s*=\s*(?<value>0x[0-9A-Fa-f]+|[0-9]+)", RegexOptions.CultureInvariant)]
    private static partial Regex _constantRegex();

    [GeneratedRegex(@"\blocal\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)\s*=\s*orderedTable\s*\(\s*\)", RegexOptions.CultureInvariant)]
    private static partial Regex _stateTableRegex();

    [GeneratedRegex(@"(?:\bfunction\s+(?<name>[A-Za-z_][A-Za-z0-9_.]*)\s*\(|\b(?<name>[A-Za-z_][A-Za-z0-9_.]*)\s*=\s*function\s*\()", RegexOptions.CultureInvariant)]
    private static partial Regex _functionRegex();

    [GeneratedRegex("dbr\\s*=\\s*\"(?<record>[^\"]+\\.dbr)\"", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex _dbrRegex();

    [GeneratedRegex(@"(?:TokenStateBasedObjectSwap|UpdateObjectSwap)\s*\([^)]*?,\s*(?<table>[A-Za-z_][A-Za-z0-9_]*)\s*\)", RegexOptions.CultureInvariant)]
    private static partial Regex _objectSwapRegex();

    [GeneratedRegex(@"\bGrantQuest\s*\(\s*(?<quest>[^,]+),\s*(?<task>[^)]+)\)", RegexOptions.CultureInvariant)]
    private static partial Regex _grantQuestRegex();
}
