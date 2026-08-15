using GdCli.Contracts;
using Microsoft.Data.Sqlite;

namespace GdCli.Database;

internal sealed class ItemSetRepository
{
    private const string _selectSql = """
        SELECT R.record_id, COALESCE(R.display_name, R.record_id), NULLIF(R.name_tag, ''), S.item_level, S.availability
        FROM item_sets S
        JOIN records R ON R.id = S.record_pk
        """;

    private readonly SqliteConnection _connection;

    public ItemSetRepository(SqliteConnection connection)
    {
        _connection = connection;
    }

    public List<ItemSetRecord> LoadForItems(IEnumerable<string> itemRecordIds)
    {
        var result = new List<ItemSetRecord>();
        foreach (var chunk in itemRecordIds.Distinct(StringComparer.OrdinalIgnoreCase).Chunk(400))
        {
            using var command = _connection.CreateCommand();
            var parameters = SqliteQuery.AddValues(command, "item", chunk);
            command.CommandText = $"""
                {_selectSql}
                WHERE S.record_pk IN (
                    SELECT DISTINCT M.set_pk
                    FROM item_set_members M
                    JOIN records IR ON IR.id = M.item_pk
                    WHERE IR.record_id IN ({parameters})
                )
                ORDER BY R.record_id COLLATE NOCASE
                """;
            result.AddRange(_read(command));
        }
        result = result
            .DistinctBy(itemSet => itemSet.RecordId, StringComparer.OrdinalIgnoreCase)
            .OrderBy(itemSet => itemSet.RecordId, StringComparer.OrdinalIgnoreCase)
            .ToList();
        _populateMembers(result);
        return result;
    }

    public Dictionary<string, List<ItemSetBonus>> LoadBonuses(IEnumerable<string> setRecordIds)
    {
        var result = new Dictionary<string, List<ItemSetBonus>>(StringComparer.OrdinalIgnoreCase);
        foreach (var chunk in setRecordIds.Distinct(StringComparer.OrdinalIgnoreCase).Chunk(400))
        {
            using var command = _connection.CreateCommand();
            var parameters = SqliteQuery.AddValues(command, "set", chunk);
            command.CommandText = $"""
                SELECT R.record_id, B.required_pieces, N.name, F.numeric_value, F.text_value
                FROM item_set_bonuses B
                JOIN records R ON R.id = B.set_pk
                JOIN record_fields F
                  ON F.record_pk = B.set_pk
                 AND F.ordinal = B.field_ordinal
                JOIN field_names N ON N.id = F.field_pk
                WHERE R.record_id IN ({parameters})
                  AND N.name NOT IN ('setMembers', 'itemSkillModifierControl')
                ORDER BY R.record_id, B.required_pieces, N.name
                """;
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var recordId = reader.GetString(0);
                if (!result.TryGetValue(recordId, out var bonuses))
                {
                    bonuses = [];
                    result[recordId] = bonuses;
                }
                var requiredPieces = reader.GetInt32(1);
                var bonus = bonuses.LastOrDefault(value => value.RequiredPieces == requiredPieces);
                if (bonus == null)
                {
                    bonus = new ItemSetBonus
                    {
                        RequiredPieces = requiredPieces
                    };
                    bonuses.Add(bonus);
                }
                bonus.Stats.Add(new RawStat
                {
                    Field = reader.GetString(2),
                    Value = reader.GetDouble(3),
                    TextValue = reader.IsDBNull(4) ? null : reader.GetString(4)
                });
            }
        }
        return result;
    }

    public Dictionary<string, List<RawStat>> LoadBonusDefinitions(IEnumerable<string> setRecordIds)
    {
        var result = new Dictionary<string, List<RawStat>>(StringComparer.OrdinalIgnoreCase);
        foreach (var chunk in setRecordIds.Distinct(StringComparer.OrdinalIgnoreCase).Chunk(400))
        {
            using var command = _connection.CreateCommand();
            var parameters = SqliteQuery.AddValues(command, "set", chunk);
            command.CommandText = $"""
                SELECT R.record_id, N.name, F.numeric_value, F.text_value
                FROM record_fields F
                JOIN records R ON R.id = F.record_pk
                JOIN field_names N ON N.id = F.field_pk
                WHERE R.record_id IN ({parameters})
                  AND F.ordinal = 0
                  AND (N.name LIKE 'augmentSkillName%'
                       OR N.name IN ('itemSkillName', 'itemSkillAutoController'))
                """;
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var recordId = reader.GetString(0);
                if (!result.TryGetValue(recordId, out var fields))
                {
                    fields = [];
                    result[recordId] = fields;
                }
                fields.Add(new RawStat
                {
                    Field = reader.GetString(1),
                    Value = reader.GetDouble(2),
                    TextValue = reader.IsDBNull(3) ? null : reader.GetString(3)
                });
            }
        }
        return result;
    }

    private void _populateMembers(List<ItemSetRecord> sets)
    {
        if (sets.Count == 0)
            return;

        var byRecord = sets.ToDictionary(set => set.RecordId, StringComparer.OrdinalIgnoreCase);
        foreach (var chunk in byRecord.Keys.Chunk(400))
        {
            using var command = _connection.CreateCommand();
            var parameters = SqliteQuery.AddValues(command, "set", chunk);
            command.CommandText = $"""
                SELECT SR.record_id, IR.record_id, COALESCE(IR.display_name, IR.record_id), I.rarity, I.item_class,
                       I.required_level, I.availability
                FROM item_set_members M
                JOIN records SR ON SR.id = M.set_pk
                JOIN items I ON I.record_pk = M.item_pk
                JOIN records IR ON IR.id = M.item_pk
                WHERE SR.record_id IN ({parameters})
                ORDER BY SR.record_id, M.ordinal
                """;
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var set = byRecord[reader.GetString(0)];
                set.Members.Add(new ItemSetMember
                {
                    RecordId = reader.GetString(1),
                    Name = reader.GetString(2),
                    Rarity = reader.GetString(3),
                    ItemClass = reader.GetString(4),
                    RequiredLevel = reader.GetDouble(5),
                    Availability = reader.GetString(6)
                });
            }
        }
    }

    private static List<ItemSetRecord> _read(SqliteCommand command)
    {
        using var reader = command.ExecuteReader();
        var result = new List<ItemSetRecord>();
        while (reader.Read())
        {
            result.Add(new ItemSetRecord
            {
                RecordId = reader.GetString(0),
                Name = reader.GetString(1),
                NameTag = reader.IsDBNull(2) ? null : reader.GetString(2),
                ItemLevel = reader.GetDouble(3),
                Availability = reader.GetString(4)
            });
        }
        return result;
    }
}
