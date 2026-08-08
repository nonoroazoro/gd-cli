using System.Globalization;
using GdCli.Contracts;
using Microsoft.Data.Sqlite;

namespace GdCli.Database;

internal sealed class ItemRepository
{
    private const string _selectSql = """
        SELECT R.record_id, I.name, NULLIF(R.name_tag, ''), I.rarity, I.item_class, I.item_level, I.required_level, I.is_mi
        FROM items I
        JOIN records R ON R.id = I.record_pk
        """;

    private const string _filterSql = """
        WHERE (@rarity IS NULL OR I.rarity = @rarity COLLATE NOCASE)
          AND (@class IS NULL OR I.item_class = @class COLLATE NOCASE)
          AND (@minimum IS NULL OR I.required_level >= @minimum)
          AND (@maximum IS NULL OR I.required_level <= @maximum)
          AND (@mi IS NULL OR I.is_mi = @mi)
        """;

    private readonly SqliteConnection _connection;

    public ItemRepository(SqliteConnection connection)
    {
        _connection = connection;
    }

    public int Count(ItemFilter filter)
    {
        using var command = _createFilterCommand($"SELECT COUNT(*) FROM items I {_filterSql}", filter);
        return Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture);
    }

    public List<ItemRecord> Load(ItemFilter filter, int offset, int? limit)
    {
        using var command = _createFilterCommand($"{_selectSql} {_filterSql} ORDER BY R.record_id COLLATE NOCASE LIMIT @limit OFFSET @offset", filter);
        SqliteQuery.AddPaging(command, offset, limit);
        return _read(command);
    }

    public ItemRecord? FindByRecordId(string recordId)
    {
        using var command = _connection.CreateCommand();
        command.CommandText = $"{_selectSql} WHERE R.record_id = @record COLLATE NOCASE LIMIT 1";
        command.Parameters.AddWithValue("@record", recordId);
        return _read(command).FirstOrDefault();
    }

    public int CountMatches(string query, bool exact, bool miOnly)
    {
        using var command = _createMatchCommand("SELECT COUNT(*) FROM items I JOIN records R ON R.id = I.record_pk", query, exact, miOnly);
        return Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture);
    }

    public List<ItemRecord> LoadMatches(string query, bool exact, bool miOnly, int offset, int? limit)
    {
        using var command = _createMatchCommand(_selectSql, query, exact, miOnly);
        command.CommandText += " ORDER BY R.record_id COLLATE NOCASE LIMIT @limit OFFSET @offset";
        SqliteQuery.AddPaging(command, offset, limit);
        return _read(command);
    }

    private SqliteCommand _createFilterCommand(string sql, ItemFilter filter)
    {
        var command = _connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("@rarity", SqliteQuery.Value(filter.Rarity));
        command.Parameters.AddWithValue("@class", SqliteQuery.Value(filter.ItemClass));
        command.Parameters.AddWithValue("@minimum", SqliteQuery.Value(filter.MinimumLevel));
        command.Parameters.AddWithValue("@maximum", SqliteQuery.Value(filter.MaximumLevel));
        command.Parameters.AddWithValue("@mi", SqliteQuery.Value(filter.IsMi));
        return command;
    }

    private SqliteCommand _createMatchCommand(string sql, string query, bool exact, bool miOnly)
    {
        var command = _connection.CreateCommand();
        var predicate = exact
            ? "(R.record_id = @query COLLATE NOCASE OR I.name = @query COLLATE NOCASE)"
            : "(R.record_id LIKE @pattern ESCAPE '\\' COLLATE NOCASE OR I.name LIKE @pattern ESCAPE '\\' COLLATE NOCASE)";
        command.CommandText = $"{sql} WHERE {predicate} AND (@miOnly = 0 OR I.is_mi = 1)";
        command.Parameters.AddWithValue("@query", query);
        command.Parameters.AddWithValue("@pattern", SqliteQuery.ContainsPattern(query));
        command.Parameters.AddWithValue("@miOnly", miOnly);
        return command;
    }

    private static List<ItemRecord> _read(SqliteCommand command)
    {
        using var reader = command.ExecuteReader();
        var result = new List<ItemRecord>();
        while (reader.Read())
        {
            result.Add(new ItemRecord
            {
                RecordId = reader.GetString(0),
                Name = reader.GetString(1),
                NameTag = reader.IsDBNull(2) ? null : reader.GetString(2),
                Rarity = reader.GetString(3),
                ItemClass = reader.GetString(4),
                ItemLevel = reader.GetDouble(5),
                RequiredLevel = reader.GetDouble(6),
                IsMi = reader.GetBoolean(7)
            });
        }
        return result;
    }
}
