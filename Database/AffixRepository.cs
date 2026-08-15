using System.Globalization;
using GdCli.Contracts;
using Microsoft.Data.Sqlite;

namespace GdCli.Database;

internal sealed class AffixRepository
{
    private const string _selectSql = """
        SELECT
            R.record_id,
            COALESCE(R.display_name, R.record_id),
            A.family,
            A.kind,
            A.rarity,
            A.item_level,
            A.required_level,
            A.jitter_percent,
            COALESCE((SELECT GROUP_CONCAT(C.category, ',')
                      FROM (SELECT DISTINCT category FROM ascended_affix_categories
                            WHERE affix_pk = A.record_pk ORDER BY category COLLATE NOCASE) C), ''),
            COALESCE((SELECT GROUP_CONCAT(G.group_name, ',')
                      FROM (SELECT DISTINCT group_name FROM ascended_affix_categories
                            WHERE affix_pk = A.record_pk ORDER BY group_name COLLATE NOCASE) G), '')
        FROM affixes A
        JOIN records R ON R.id = A.record_pk
        """;

    private const string _filterSql = """
        WHERE A.family IN ('standard', 'ascended')
          AND (@family IS NULL OR A.family = @family COLLATE NOCASE)
          AND (@rarity IS NULL OR A.rarity = @rarity COLLATE NOCASE)
          AND (@kind IS NULL OR A.kind = @kind COLLATE NOCASE)
          AND (@minimum IS NULL OR A.required_level >= @minimum)
          AND (@maximum IS NULL OR A.required_level <= @maximum)
          AND (@query IS NULL OR
               (@exactQuery = 1 AND
                    (R.record_id = @query COLLATE NOCASE OR
                     R.display_name = @query COLLATE NOCASE)) OR
               (@exactQuery = 0 AND
                    (R.record_id LIKE @pattern ESCAPE '\' COLLATE NOCASE OR
                     R.display_name LIKE @pattern ESCAPE '\' COLLATE NOCASE)))
          AND ((@class IS NULL AND @category IS NULL) OR
               (@class IS NOT NULL AND A.family = 'standard' AND EXISTS (
                   SELECT 1 FROM affix_item_classes C
                   WHERE C.affix_pk = A.record_pk AND C.item_class = @class COLLATE NOCASE)) OR
               (@category IS NOT NULL AND A.family = 'ascended' AND EXISTS (
                   SELECT 1 FROM ascended_affix_categories C
                   WHERE C.affix_pk = A.record_pk AND C.category = @category COLLATE NOCASE)))
        """;

    private readonly SqliteConnection _connection;

    public AffixRepository(SqliteConnection connection)
    {
        _connection = connection;
    }

    public int Count(AffixFilter filter)
    {
        using var command = _createFilterCommand($"SELECT COUNT(*) FROM affixes A JOIN records R ON R.id = A.record_pk {_filterSql}", filter);
        return Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture);
    }

    public List<AffixRecord> Load(AffixFilter filter, int offset, int? limit)
    {
        using var command = _createFilterCommand(
            $"{_selectSql} {_filterSql} ORDER BY R.record_id COLLATE NOCASE LIMIT @limit OFFSET @offset",
            filter);
        SqliteQuery.AddPaging(command, offset, limit);
        return _read(command);
    }

    private SqliteCommand _createFilterCommand(string sql, AffixFilter filter)
    {
        var command = _connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("@family", SqliteQuery.Value(filter.Family));
        command.Parameters.AddWithValue("@rarity", SqliteQuery.Value(filter.Rarity));
        command.Parameters.AddWithValue("@kind", SqliteQuery.Value(filter.Kind));
        command.Parameters.AddWithValue("@class", SqliteQuery.Value(filter.ItemClass));
        command.Parameters.AddWithValue("@category", SqliteQuery.Value(filter.Category));
        command.Parameters.AddWithValue("@minimum", SqliteQuery.Value(filter.MinimumLevel));
        command.Parameters.AddWithValue("@maximum", SqliteQuery.Value(filter.MaximumLevel));
        command.Parameters.AddWithValue("@query", SqliteQuery.Value(filter.Query));
        command.Parameters.AddWithValue("@exactQuery", filter.ExactQuery);
        command.Parameters.AddWithValue(
            "@pattern",
            filter.Query == null ? DBNull.Value : SqliteQuery.ContainsPattern(filter.Query));
        return command;
    }

    private static List<AffixRecord> _read(SqliteCommand command)
    {
        using var reader = command.ExecuteReader();
        var result = new List<AffixRecord>();
        while (reader.Read())
        {
            var family = reader.GetString(2);
            result.Add(new AffixRecord
            {
                RecordId = reader.GetString(0),
                Name = reader.GetString(1),
                Family = family,
                Kind = reader.IsDBNull(3) ? null : reader.GetString(3),
                Rarity = reader.GetString(4),
                ItemLevel = reader.GetDouble(5),
                RequiredLevel = reader.GetDouble(6),
                JitterPercent = reader.GetDouble(7),
                Categories = family == "ascended" ? _split(reader.GetString(8)) : null,
                Groups = family == "ascended" ? _split(reader.GetString(9)) : null
            });
        }
        return result;
    }

    private static string[] _split(string value)
    {
        return string.IsNullOrEmpty(value)
            ? []
            : value.Split(',', StringSplitOptions.RemoveEmptyEntries);
    }
}
