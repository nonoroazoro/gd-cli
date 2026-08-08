using Microsoft.Data.Sqlite;

namespace GdCli.Database;

internal static class SqliteQuery
{
    public static void AddPaging(SqliteCommand command, int offset, int? limit)
    {
        command.Parameters.AddWithValue("@offset", offset);
        command.Parameters.AddWithValue("@limit", limit ?? -1);
    }

    public static string AddValues(SqliteCommand command, string prefix, string[] values)
    {
        var names = new string[values.Length];
        for (var index = 0; index < values.Length; index++)
        {
            names[index] = $"@{prefix}{index}";
            command.Parameters.AddWithValue(names[index], values[index]);
        }
        return string.Join(',', names);
    }

    public static string ContainsPattern(string value)
    {
        return $"%{value.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("%", "\\%", StringComparison.Ordinal).Replace("_", "\\_", StringComparison.Ordinal)}%";
    }

    public static object Value(object? value)
    {
        return value ?? DBNull.Value;
    }
}
