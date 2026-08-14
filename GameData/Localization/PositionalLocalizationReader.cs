using System.Text;
using GdCli.GameData.Arc;

namespace GdCli.GameData.Localization;

internal static class PositionalLocalizationReader
{
    public static IReadOnlyDictionary<string, IReadOnlyList<string>> Read(string path)
    {
        using var archive = new ArcArchive(path);
        var result = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in archive.Entries.Where(entry => entry.Path.EndsWith(".txt", StringComparison.OrdinalIgnoreCase)))
        {
            var text = Encoding.UTF8.GetString(archive.ReadEntry(entry.Path));
            if (text.Length > 0 && text[0] == '\uFEFF')
                text = text[1..];
            var lines = text
                .Replace("\r\n", "\n", StringComparison.Ordinal)
                .Replace('\r', '\n')
                .Split('\n');
            var length = lines.Length;
            while (length > 0 && lines[length - 1].Length == 0)
                length--;
            if (length == 0 || _isTagFile(lines, length))
                continue;
            result[_key(entry.Path)] = lines.Take(length).ToArray();
        }
        return result;
    }

    private static bool _isTagFile(string[] lines, int length)
    {
        for (var index = 0; index < length; index++)
        {
            if (lines[index].Length == 0)
                continue;
            return lines[index].StartsWith("tag", StringComparison.OrdinalIgnoreCase) &&
                   lines[index].Contains('=');
        }
        return false;
    }

    private static string _key(string path)
    {
        var normalized = path.Replace('\\', '/');
        return normalized[..normalized.LastIndexOf('.')].ToLowerInvariant();
    }
}
