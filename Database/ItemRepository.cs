using System.Globalization;
using GdCli.Contracts;
using Microsoft.Data.Sqlite;

namespace GdCli.Database;

internal sealed class ItemRepository
{
    private const string _selectSql = """
        SELECT R.record_id, COALESCE(R.display_name, R.record_id), NULLIF(R.name_tag, ''), I.rarity, I.item_class,
               I.item_level, I.required_level, I.is_mi, I.availability
        FROM items I
        JOIN records R ON R.id = I.record_pk
        """;

    private const string _filterSql = """
        WHERE (@rarity IS NULL OR I.rarity = @rarity COLLATE NOCASE)
          AND (@class IS NULL OR I.item_class = @class COLLATE NOCASE)
          AND (@minimum IS NULL OR I.required_level >= @minimum)
          AND (@maximum IS NULL OR I.required_level <= @maximum)
          AND (@mi IS NULL OR I.is_mi = @mi)
          AND (@query IS NULL OR
               (@exactQuery = 1 AND
                    (R.record_id = @query COLLATE NOCASE OR
                     R.display_name = @query COLLATE NOCASE OR
                     EXISTS (
                         SELECT 1
                         FROM item_set_members M
                         JOIN records SR ON SR.id = M.set_pk
                          WHERE M.item_pk = I.record_pk
                            AND (SR.record_id = @query COLLATE NOCASE OR
                                SR.display_name = @query COLLATE NOCASE)))) OR
               (@exactQuery = 0 AND
                    (R.record_id LIKE @pattern ESCAPE '\' COLLATE NOCASE OR
                     R.display_name LIKE @pattern ESCAPE '\' COLLATE NOCASE OR
                     EXISTS (
                         SELECT 1
                         FROM item_set_members M
                         JOIN records SR ON SR.id = M.set_pk
                         WHERE M.item_pk = I.record_pk
                           AND (SR.record_id LIKE @pattern ESCAPE '\' COLLATE NOCASE OR
                                SR.display_name LIKE @pattern ESCAPE '\' COLLATE NOCASE)))))
          AND ((@availability IS NOT NULL AND I.availability = @availability COLLATE NOCASE)
               OR (@availability IS NULL AND (@includeUnavailable = 1 OR I.availability <> 'unavailable')))
        """;

    private readonly SqliteConnection _connection;

    public ItemRepository(SqliteConnection connection)
    {
        _connection = connection;
    }

    public int Count(ItemFilter filter)
    {
        using var command = _createFilterCommand(
            $"SELECT COUNT(*) FROM items I JOIN records R ON R.id = I.record_pk {_filterSql}",
            filter);
        return Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture);
    }

    public List<ItemRecord> Load(ItemFilter filter, int offset, int? limit)
    {
        using var command = _createFilterCommand($"{_selectSql} {_filterSql} ORDER BY R.record_id COLLATE NOCASE LIMIT @limit OFFSET @offset", filter);
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
        command.Parameters.AddWithValue("@availability", SqliteQuery.Value(filter.Availability));
        command.Parameters.AddWithValue("@includeUnavailable", filter.IncludeUnavailable);
        command.Parameters.AddWithValue("@query", SqliteQuery.Value(filter.Query));
        command.Parameters.AddWithValue("@exactQuery", filter.ExactQuery);
        command.Parameters.AddWithValue(
            "@pattern",
            filter.Query == null ? DBNull.Value : SqliteQuery.ContainsPattern(filter.Query));
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
                IsMi = reader.GetBoolean(7),
                Availability = reader.GetString(8)
            });
        }
        return result;
    }
}
