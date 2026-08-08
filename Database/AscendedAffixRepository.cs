using System.Globalization;
using GdCli.Contracts;
using Microsoft.Data.Sqlite;

namespace GdCli.Database;

internal sealed class AscendedAffixRepository
{
    private const string _selectSql = """
        SELECT
            R.record_id,
            R.display_name,
            COALESCE((SELECT GROUP_CONCAT(C.category, ',')
                      FROM (SELECT DISTINCT category FROM ascended_affix_categories
                            WHERE affix_pk = A.record_pk ORDER BY category COLLATE NOCASE) C), ''),
            COALESCE((SELECT GROUP_CONCAT(G.group_name, ',')
                      FROM (SELECT DISTINCT group_name FROM ascended_affix_categories
                            WHERE affix_pk = A.record_pk ORDER BY group_name COLLATE NOCASE) G), '')
        FROM ascended_affixes A
        JOIN records R ON R.id = A.record_pk
        """;

    private const string _filterSql = """
        WHERE (@category IS NULL OR EXISTS (
                  SELECT 1 FROM ascended_affix_categories C
                  WHERE C.affix_pk = A.record_pk AND C.category = @category COLLATE NOCASE))
        """;

    private readonly SqliteConnection _connection;

    public AscendedAffixRepository(SqliteConnection connection)
    {
        _connection = connection;
    }

    public int Count(AscendedAffixFilter filter)
    {
        using var command = _createFilterCommand(
            $"SELECT COUNT(*) FROM ascended_affixes A {_filterSql}",
            filter);
        return Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture);
    }

    public List<AscendedAffixRecord> Load(AscendedAffixFilter filter, int offset, int? limit)
    {
        using var command = _createFilterCommand(
            $"{_selectSql} {_filterSql} ORDER BY R.record_id COLLATE NOCASE LIMIT @limit OFFSET @offset",
            filter);
        SqliteQuery.AddPaging(command, offset, limit);
        return _read(command);
    }

    public AscendedAffixRecord? FindByRecordId(string recordId)
    {
        using var command = _connection.CreateCommand();
        command.CommandText = $"{_selectSql} WHERE R.record_id = @record COLLATE NOCASE LIMIT 1";
        command.Parameters.AddWithValue("@record", recordId);
        return _read(command).FirstOrDefault();
    }

    public Dictionary<string, List<AscendedSkillModifier>> LoadSkillModifiers(
        IEnumerable<string> recordIds)
    {
        var result = new Dictionary<string, List<AscendedSkillModifier>>(StringComparer.OrdinalIgnoreCase);
        foreach (var chunk in recordIds.Distinct(StringComparer.OrdinalIgnoreCase).Chunk(400))
        {
            using var command = _connection.CreateCommand();
            var parameters = SqliteQuery.AddValues(command, "record", chunk);
            command.CommandText = $"""
                SELECT A.record_id, M.record_id, M.display_name
                FROM ascended_skill_modifiers ASM
                JOIN records A ON A.id = ASM.affix_pk
                JOIN records M ON M.id = ASM.modifier_pk
                WHERE A.record_id IN ({parameters})
                ORDER BY A.record_id, M.record_id
                """;
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var affixRecordId = reader.GetString(0);
                if (!result.TryGetValue(affixRecordId, out var modifiers))
                {
                    modifiers = [];
                    result[affixRecordId] = modifiers;
                }
                modifiers.Add(new AscendedSkillModifier
                {
                    RecordId = reader.GetString(1),
                    Name = reader.GetString(2)
                });
            }
        }
        return result;
    }

    private SqliteCommand _createFilterCommand(string sql, AscendedAffixFilter filter)
    {
        var command = _connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("@category", SqliteQuery.Value(filter.Category));
        return command;
    }

    private static List<AscendedAffixRecord> _read(SqliteCommand command)
    {
        using var reader = command.ExecuteReader();
        var result = new List<AscendedAffixRecord>();
        while (reader.Read())
        {
            result.Add(new AscendedAffixRecord
            {
                RecordId = reader.GetString(0),
                Name = reader.GetString(1),
                Categories = _split(reader.GetString(2)),
                Groups = _split(reader.GetString(3))
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
