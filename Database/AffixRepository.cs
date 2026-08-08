using System.Globalization;
using GdCli.Contracts;
using Microsoft.Data.Sqlite;

namespace GdCli.Database;

internal sealed class AffixRepository
{
    private const string _selectSql = """
        SELECT R.record_id, A.name, A.kind, A.rarity, A.item_level, A.required_level, A.jitter_percent
        FROM affixes A
        JOIN records R ON R.id = A.record_pk
        """;

    private const string _filterSql = """
        WHERE (@rarity IS NULL OR A.rarity = @rarity COLLATE NOCASE)
          AND (@kind IS NULL OR A.kind = @kind COLLATE NOCASE)
          AND (@class IS NULL OR EXISTS (
                  SELECT 1 FROM affix_item_classes C
                  WHERE C.affix_pk = A.record_pk AND C.item_class = @class COLLATE NOCASE))
          AND (@minimum IS NULL OR A.required_level >= @minimum)
          AND (@maximum IS NULL OR A.required_level <= @maximum)
        """;

    private readonly SqliteConnection _connection;

    public AffixRepository(SqliteConnection connection)
    {
        _connection = connection;
    }

    public int Count(AffixFilter filter)
    {
        using var command = _createFilterCommand($"SELECT COUNT(*) FROM affixes A {_filterSql}", filter);
        return Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture);
    }

    public List<AffixRecord> Load(AffixFilter filter, int offset, int? limit)
    {
        using var command = _createFilterCommand($"{_selectSql} {_filterSql} ORDER BY R.record_id COLLATE NOCASE LIMIT @limit OFFSET @offset", filter);
        SqliteQuery.AddPaging(command, offset, limit);
        return _read(command);
    }

    public AffixRecord? FindByRecordId(string recordId)
    {
        using var command = _connection.CreateCommand();
        command.CommandText = $"{_selectSql} WHERE R.record_id = @record COLLATE NOCASE LIMIT 1";
        command.Parameters.AddWithValue("@record", recordId);
        return _read(command).FirstOrDefault();
    }

    private SqliteCommand _createFilterCommand(string sql, AffixFilter filter)
    {
        var command = _connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("@rarity", SqliteQuery.Value(filter.Rarity));
        command.Parameters.AddWithValue("@kind", SqliteQuery.Value(filter.Kind));
        command.Parameters.AddWithValue("@class", SqliteQuery.Value(filter.ItemClass));
        command.Parameters.AddWithValue("@minimum", SqliteQuery.Value(filter.MinimumLevel));
        command.Parameters.AddWithValue("@maximum", SqliteQuery.Value(filter.MaximumLevel));
        return command;
    }

    private static List<AffixRecord> _read(SqliteCommand command)
    {
        using var reader = command.ExecuteReader();
        var result = new List<AffixRecord>();
        while (reader.Read())
        {
            result.Add(new AffixRecord
            {
                RecordId = reader.GetString(0),
                Name = reader.GetString(1),
                Kind = reader.GetString(2),
                Rarity = reader.GetString(3),
                ItemLevel = reader.GetDouble(4),
                RequiredLevel = reader.GetDouble(5),
                JitterPercent = reader.GetDouble(6)
            });
        }
        return result;
    }
}
