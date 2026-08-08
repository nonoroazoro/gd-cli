using System.Text;
using GdCli.GameData.Arc;

namespace GdCli.GameData.Tags;

internal static class TagArchiveReader
{
    public static IReadOnlyDictionary<string, string> Read(string path)
    {
        using var archive = new ArcArchive(path);
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var entry in archive.Entries.Where(entry => entry.Path.EndsWith(".txt", StringComparison.OrdinalIgnoreCase)))
        {
            var text = Encoding.UTF8.GetString(archive.ReadEntry(entry.Path));
            if (text.Length > 0 && text[0] == '\uFEFF')
                text = text[1..];
            foreach (var rawLine in text.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n').Split('\n'))
            {
                var separator = rawLine.IndexOf('=');
                if (separator <= 0)
                    continue;
                var tag = rawLine[..separator].Trim();
                if (!tag.StartsWith("tag", StringComparison.OrdinalIgnoreCase))
                    continue;
                result[tag] = _removeControlCodes(rawLine[(separator + 1)..]);
            }
        }
        return result;
    }

    private static string _removeControlCodes(string value)
    {
        var result = new StringBuilder(value.Length);
        for (var index = 0; index < value.Length; index++)
        {
            if (value[index] == '^' && index + 1 < value.Length)
            {
                index++;
                continue;
            }
            result.Append(value[index]);
        }
        return result.ToString();
    }
}
