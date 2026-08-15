using GdCli.Contracts;
using Microsoft.Data.Sqlite;

namespace GdCli.Database;

internal sealed class AcquisitionRepository
{
    private readonly SqliteConnection _connection;

    public AcquisitionRepository(SqliteConnection connection)
    {
        _connection = connection;
    }

    public Dictionary<string, List<AcquisitionSourceRecord>> LoadSources(IEnumerable<string> itemRecords)
    {
        var result = new Dictionary<string, List<AcquisitionSourceRecord>>(StringComparer.OrdinalIgnoreCase);
        var records = itemRecords.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        foreach (var chunk in records.Chunk(400))
        {
            using var command = _connection.CreateCommand();
            var parameters = SqliteQuery.AddValues(command, "item", chunk);
            command.CommandText = $"""
                SELECT I.record_id, A.kind, S.record_id, S.display_name, S.name_tag
                FROM acquisition_sources A
                JOIN records I ON I.id = A.item_pk
                LEFT JOIN records S ON S.id = A.source_pk
                WHERE I.record_id IN ({parameters})
                ORDER BY I.record_id,
                    CASE A.kind
                        WHEN 'vendor' THEN 1
                        WHEN 'specificMonster' THEN 2
                        ELSE 3
                    END,
                    S.display_name COLLATE NOCASE,
                    S.record_id
                """;
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var itemRecord = reader.GetString(0);
                if (!result.TryGetValue(itemRecord, out var sources))
                {
                    sources = [];
                    result[itemRecord] = sources;
                }
                sources.Add(new AcquisitionSourceRecord
                {
                    Kind = reader.GetString(1),
                    RecordId = reader.IsDBNull(2) ? null : reader.GetString(2),
                    Name = reader.IsDBNull(3) ? null : reader.GetString(3),
                    NameTag = reader.IsDBNull(4) ? null : reader.GetString(4)
                });
            }
        }
        _loadContainerSources(records, result);
        return result;
    }

    private void _loadContainerSources(
        IReadOnlyList<string> itemRecords,
        Dictionary<string, List<AcquisitionSourceRecord>> result)
    {
        foreach (var chunk in itemRecords.Chunk(100))
        {
            using var command = _connection.CreateCommand();
            var parameters = SqliteQuery.AddValues(command, "item", chunk);
            command.CommandText = $"""
                WITH RECURSIVE ContainerRoutes(item_pk, record_pk) AS (
                    SELECT id, id
                    FROM records
                    WHERE record_id IN ({parameters})
                    UNION
                    SELECT G.item_pk, E.source_pk
                    FROM ContainerRoutes G
                    JOIN specific_container_edges E ON E.target_pk = G.record_pk
                    WHERE NOT EXISTS (
                        SELECT 1
                        FROM random_drop_nodes D
                        WHERE D.record_pk = E.source_pk
                    )
                )
                SELECT I.record_id, S.record_id, S.display_name, S.name_tag
                FROM ContainerRoutes G
                JOIN records I ON I.id = G.item_pk
                JOIN records S ON S.id = G.record_pk
                WHERE S.class = 'FixedItemContainer'
                   OR S.template LIKE '%/fixeditemcontainer.tpl'
                ORDER BY I.record_id, S.display_name COLLATE NOCASE, S.record_id
                """;
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var itemRecord = reader.GetString(0);
                if (!result.TryGetValue(itemRecord, out var sources))
                {
                    sources = [];
                    result[itemRecord] = sources;
                }
                sources.Add(new AcquisitionSourceRecord
                {
                    Kind = GdCli.Features.Acquisition.AcquisitionKind.Container,
                    RecordId = reader.GetString(1),
                    Name = reader.IsDBNull(2) ? null : reader.GetString(2),
                    NameTag = reader.IsDBNull(3) ? null : reader.GetString(3)
                });
            }
        }
    }

    public Dictionary<string, List<ItemSummary>> LoadRecipes(IEnumerable<string> resultItemRecords)
    {
        var result = new Dictionary<string, List<ItemSummary>>(StringComparer.OrdinalIgnoreCase);
        foreach (var chunk in resultItemRecords.Distinct(StringComparer.OrdinalIgnoreCase).Chunk(400))
        {
            using var command = _connection.CreateCommand();
            var parameters = SqliteQuery.AddValues(command, "item", chunk);
            command.CommandText = $"""
                SELECT R.record_id, B.record_id, COALESCE(B.display_name, B.record_id), B.name_tag, BI.rarity, BI.item_class, BI.availability
                FROM recipes P
                JOIN records R ON R.id = P.result_item_pk
                JOIN records B ON B.id = P.recipe_item_pk
                JOIN items BI ON BI.record_pk = B.id
                WHERE R.record_id IN ({parameters})
                ORDER BY R.record_id, B.record_id
                """;
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var itemRecord = reader.GetString(0);
                if (!result.TryGetValue(itemRecord, out var recipes))
                {
                    recipes = [];
                    result[itemRecord] = recipes;
                }
                recipes.Add(new ItemSummary
                {
                    RecordId = reader.GetString(1),
                    Name = reader.GetString(2),
                    NameTag = reader.IsDBNull(3) ? null : reader.GetString(3),
                    Rarity = reader.GetString(4),
                    ItemClass = reader.GetString(5),
                    Availability = reader.GetString(6)
                });
            }
        }
        return result;
    }

    public Dictionary<string, List<MonsterSource>> LoadMiSources(IEnumerable<string> itemRecords)
    {
        var result = new Dictionary<string, List<MonsterSource>>(StringComparer.OrdinalIgnoreCase);
        foreach (var chunk in itemRecords.Distinct(StringComparer.OrdinalIgnoreCase).Chunk(400))
        {
            using var command = _connection.CreateCommand();
            var parameters = SqliteQuery.AddValues(command, "item", chunk);
            command.CommandText = $"""
                SELECT R.record_id, S.record_id, COALESCE(S.display_name, S.record_id)
                FROM acquisition_sources A
                JOIN items I ON I.record_pk = A.item_pk AND I.is_mi = 1
                JOIN records R ON R.id = I.record_pk
                JOIN records S ON S.id = A.source_pk
                WHERE A.kind = 'specificMonster'
                  AND R.record_id IN ({parameters})
                ORDER BY R.record_id, S.display_name COLLATE NOCASE, S.record_id
                """;
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var itemRecord = reader.GetString(0);
                if (!result.TryGetValue(itemRecord, out var sources))
                {
                    sources = [];
                    result[itemRecord] = sources;
                }
                sources.Add(new MonsterSource
                {
                    RecordId = reader.GetString(1),
                    Name = reader.GetString(2)
                });
            }
        }
        return result;
    }

    public IReadOnlyList<LootReference> LoadReverseLootReferences(string targetRecordId)
    {
        using var command = _connection.CreateCommand();
        command.CommandText = """
            SELECT S.record_id, COALESCE(S.display_name, S.record_id), COALESCE(S.class, ''), N.name
            FROM record_references RR
            JOIN records S ON S.id = RR.source_pk
            JOIN records T ON T.id = RR.target_pk
            JOIN field_names N ON N.id = RR.field_pk
            WHERE T.record_id = @target
              AND (S.template LIKE '%loot%.tpl'
                   OR S.class = 'FixedItemContainer'
                   OR S.record_id LIKE '%/loottables/%'
                   OR S.record_id LIKE 'records/creatures/%'
                   OR S.record_id LIKE 'records/proxies/%')
            ORDER BY S.record_id, N.name, RR.ordinal
            """;
        command.Parameters.AddWithValue("@target", targetRecordId);
        using var reader = command.ExecuteReader();
        var result = new List<LootReference>();
        while (reader.Read())
        {
            result.Add(new LootReference
            {
                SourceRecordId = reader.GetString(0),
                SourceName = reader.GetString(1),
                SourceClass = reader.GetString(2),
                Field = reader.GetString(3)
            });
        }
        return result;
    }

    public IReadOnlyList<LootCondition> LoadLootConditions(string recordId)
    {
        using var command = _connection.CreateCommand();
        command.CommandText = """
            SELECT N.name, C.numeric_value, C.text_value
            FROM loot_conditions C
            JOIN records R ON R.id = C.record_pk
            JOIN field_names N ON N.id = C.field_pk
            WHERE R.record_id = @record
            ORDER BY N.name, C.ordinal
            """;
        command.Parameters.AddWithValue("@record", recordId);
        using var reader = command.ExecuteReader();
        var result = new List<LootCondition>();
        while (reader.Read())
        {
            result.Add(new LootCondition
            {
                Field = reader.GetString(0),
                Value = reader.GetDouble(1),
                TextValue = reader.IsDBNull(2) ? null : reader.GetString(2)
            });
        }
        return result;
    }

    public IReadOnlyList<AcquisitionLocation> LoadLocations(string recordId)
    {
        using var command = _connection.CreateCommand();
        command.CommandText = """
            SELECT L.source_name, L.level_path, L.rift_gate_record_id,
                   R.record_id, P.world_x, P.world_y, P.world_z
            FROM placements P
            JOIN levels L ON L.id = P.level_pk
            JOIN records R ON R.id = P.record_pk
            WHERE R.record_id = @record
            ORDER BY L.source_name, L.level_path, P.entity_ordinal
            """;
        command.Parameters.AddWithValue("@record", recordId);
        using var reader = command.ExecuteReader();
        var result = new List<AcquisitionLocation>();
        while (reader.Read())
        {
            result.Add(new AcquisitionLocation
            {
                Source = reader.GetString(0),
                Level = reader.GetString(1),
                RiftGateRecordId = reader.GetString(2),
                PlacedRecordId = reader.GetString(3),
                X = reader.GetDouble(4),
                Y = reader.GetDouble(5),
                Z = reader.GetDouble(6)
            });
        }
        return result;
    }

    public IReadOnlyList<AcquisitionLocation> LoadEntityLocations(string recordId)
    {
        using var command = _connection.CreateCommand();
        command.CommandText = """
            WITH RECURSIVE LocationRecords(record_pk) AS (
                SELECT id
                FROM records
                WHERE record_id = @record
                UNION
                SELECT RR.source_pk
                FROM LocationRecords G
                JOIN record_references RR ON RR.target_pk = G.record_pk
                JOIN records S ON S.id = RR.source_pk
                WHERE S.record_id LIKE 'records/proxies/%'
                   OR S.record_id LIKE 'records/creatures/%'
            )
            SELECT L.source_name, L.level_path, L.rift_gate_record_id,
                   R.record_id, P.world_x, P.world_y, P.world_z
            FROM LocationRecords G
            JOIN placements P ON P.record_pk = G.record_pk
            JOIN levels L ON L.id = P.level_pk
            JOIN records R ON R.id = P.record_pk
            ORDER BY L.source_name, L.level_path, R.record_id, P.entity_ordinal
            """;
        command.Parameters.AddWithValue("@record", recordId);
        using var reader = command.ExecuteReader();
        var result = new List<AcquisitionLocation>();
        while (reader.Read())
        {
            result.Add(new AcquisitionLocation
            {
                Source = reader.GetString(0),
                Level = reader.GetString(1),
                RiftGateRecordId = reader.GetString(2),
                PlacedRecordId = reader.GetString(3),
                X = reader.GetDouble(4),
                Y = reader.GetDouble(5),
                Z = reader.GetDouble(6)
            });
        }
        return result;
    }
}
