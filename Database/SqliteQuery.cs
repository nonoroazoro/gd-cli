using Microsoft.Data.Sqlite;

namespace GdCli.Database;

internal static class SqliteQuery
{
    public static void AddPaging(SqliteCommand command, int offset, int? limit)
    {
        command.Parameters.AddWithValue("@offset", offset);
        command.Parameters.AddWithValue("@limit", limit ?? -1);
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
