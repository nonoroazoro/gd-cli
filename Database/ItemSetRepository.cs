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
