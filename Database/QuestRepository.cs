using System.Globalization;
using GdCli.Contracts;
using Microsoft.Data.Sqlite;

namespace GdCli.Database;

internal sealed class QuestRepository
{
    private readonly SqliteConnection _connection;
    private readonly QuestDetailLoader _details;

    public QuestRepository(SqliteConnection connection)
    {
        _connection = connection;
        _details = new QuestDetailLoader(_connection);
    }

    public int Count()
    {
        using var command = _connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM quests";
        return Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture);
    }

    public List<QuestRecord> Load(int offset, int? limit)
    {
        return _load(string.Empty, null, offset, limit);
    }

    public int CountMatches(string query, bool exact)
    {
        using var command = _connection.CreateCommand();
        command.CommandText = exact
            ? "SELECT COUNT(*) FROM quests WHERE quest_path = @query OR name = @query COLLATE NOCASE"
            : "SELECT COUNT(*) FROM quests WHERE quest_path LIKE @query ESCAPE '\\' OR name LIKE @query ESCAPE '\\'";
        command.Parameters.AddWithValue("@query", exact ? query : $"%{_escapeLike(query)}%");
        return Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture);
    }

    public List<QuestRecord> LoadMatches(string query, bool exact, int offset, int? limit)
    {
        var where = exact
            ? "WHERE Q.quest_path = @query OR Q.name = @query COLLATE NOCASE"
            : "WHERE Q.quest_path LIKE @query ESCAPE '\\' OR Q.name LIKE @query ESCAPE '\\'";
        return _load(where, exact ? query : $"%{_escapeLike(query)}%", offset, limit);
    }

    public void PopulateDetails(IReadOnlyList<QuestRecord> quests)
    {
        _details.Populate(quests);
    }

    private List<QuestRecord> _load(string where, string? query, int offset, int? limit)
    {
        using var command = _connection.CreateCommand();
        command.CommandText = $"""
            SELECT
                Q.quest_path, Q.source_name, Q.uid, Q.flags, Q.region, Q.name,
                (SELECT COUNT(*) FROM quest_nodes N WHERE N.quest_pk = Q.id AND N.kind = 'task'),
                (SELECT COUNT(*) FROM quest_nodes N WHERE N.quest_pk = Q.id),
                (
                    SELECT COUNT(*)
                    FROM (
                        SELECT E.role, E.record_pk
                        FROM quest_entities E
                        WHERE E.quest_pk = Q.id
                        GROUP BY E.role, E.record_pk
                    )
                )
            FROM quests Q
            {where}
            ORDER BY Q.name COLLATE NOCASE, Q.quest_path
            LIMIT @limit OFFSET @offset
            """;
        if (query != null)
            command.Parameters.AddWithValue("@query", query);
        command.Parameters.AddWithValue("@limit", limit ?? -1);
        command.Parameters.AddWithValue("@offset", offset);
        using var reader = command.ExecuteReader();
        var result = new List<QuestRecord>();
        while (reader.Read())
        {
            result.Add(new QuestRecord
            {
                QuestPath = reader.GetString(0),
                Source = reader.GetString(1),
                Uid = reader.GetInt64(2),
                Flags = reader.GetInt64(3),
                Region = reader.GetString(4),
                Name = reader.GetString(5),
                TaskCount = reader.GetInt64(6),
                NodeCount = reader.GetInt64(7),
                EntityCount = reader.GetInt64(8)
            });
        }
        return result;
    }

    private static string _escapeLike(string value)
    {
        return value.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal);
    }
}
