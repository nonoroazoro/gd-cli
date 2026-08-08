using System.Globalization;
using GdCli.Contracts;
using Microsoft.Data.Sqlite;

namespace GdCli.Database;

internal sealed class SearchRepository
{
    private readonly SqliteConnection _connection;

    public SearchRepository(SqliteConnection connection)
    {
        _connection = connection;
    }

    public int Count(SearchFilter filter)
    {
        using var command = _createCommand(filter, false);
        command.CommandText = $"SELECT COUNT(*) FROM ({command.CommandText}) AS matches";
        return Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture);
    }

    public List<SearchHit> Load(SearchFilter filter, int offset, int? limit)
    {
        using var command = _createCommand(filter, true);
        command.CommandText += " ORDER BY entity COLLATE NOCASE, record_id COLLATE NOCASE LIMIT @limit OFFSET @offset";
        SqliteQuery.AddPaging(command, offset, limit);
        using var reader = command.ExecuteReader();
        var result = new List<SearchHit>();
        while (reader.Read())
        {
            result.Add(new SearchHit
            {
                Entity = reader.GetString(0),
                RecordId = reader.GetString(1),
                Name = reader.GetString(2),
                Rarity = reader.GetString(3),
                ItemClass = reader.IsDBNull(4) ? null : reader.GetString(4),
                Kind = reader.IsDBNull(5) ? null : reader.GetString(5),
                ItemLevel = reader.GetDouble(6),
                RequiredLevel = reader.GetDouble(7)
            });
        }
        return result;
    }

    private SqliteCommand _createCommand(SearchFilter filter, bool projectData)
    {
        var command = _connection.CreateCommand();
        var branches = new List<string>();
        if (filter.Kind == null)
        {
            var projection = projectData
                ? "'item' AS entity, R.record_id, I.name, I.rarity, I.item_class, NULL AS kind, I.item_level, I.required_level"
                : "1 AS match";
            branches.Add($"""
                SELECT {projection}
                FROM items I
                JOIN records R ON R.id = I.record_pk
                WHERE (I.name LIKE @pattern ESCAPE '\' COLLATE NOCASE OR R.record_id LIKE @pattern ESCAPE '\' COLLATE NOCASE)
                  AND (@rarity IS NULL OR I.rarity = @rarity COLLATE NOCASE)
                  AND (@class IS NULL OR I.item_class = @class COLLATE NOCASE)
                  AND (@minimum IS NULL OR I.required_level >= @minimum)
                  AND (@maximum IS NULL OR I.required_level <= @maximum)
                """);
        }
        if (filter.ItemClass == null)
        {
            var projection = projectData
                ? "'affix' AS entity, R.record_id, A.name, A.rarity, NULL AS item_class, A.kind, A.item_level, A.required_level"
                : "1 AS match";
            branches.Add($"""
                SELECT {projection}
                FROM affixes A
                JOIN records R ON R.id = A.record_pk
                WHERE (A.name LIKE @pattern ESCAPE '\' COLLATE NOCASE OR R.record_id LIKE @pattern ESCAPE '\' COLLATE NOCASE)
                  AND (@rarity IS NULL OR A.rarity = @rarity COLLATE NOCASE)
                  AND (@kind IS NULL OR A.kind = @kind COLLATE NOCASE)
                  AND (@minimum IS NULL OR A.required_level >= @minimum)
                  AND (@maximum IS NULL OR A.required_level <= @maximum)
                """);
        }
        command.CommandText = string.Join(" UNION ALL ", branches);
        command.Parameters.AddWithValue("@pattern", SqliteQuery.ContainsPattern(filter.Query));
        command.Parameters.AddWithValue("@rarity", SqliteQuery.Value(filter.Rarity));
        command.Parameters.AddWithValue("@class", SqliteQuery.Value(filter.ItemClass));
        command.Parameters.AddWithValue("@kind", SqliteQuery.Value(filter.Kind));
        command.Parameters.AddWithValue("@minimum", SqliteQuery.Value(filter.MinimumLevel));
        command.Parameters.AddWithValue("@maximum", SqliteQuery.Value(filter.MaximumLevel));
        return command;
    }
}
