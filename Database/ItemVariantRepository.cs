using GdCli.Contracts;
using Microsoft.Data.Sqlite;

namespace GdCli.Database;

internal sealed class ItemVariantRepository
{
    private readonly SqliteConnection _connection;

    public ItemVariantRepository(SqliteConnection connection)
    {
        _connection = connection;
    }

    public Dictionary<string, List<ItemVariantRecord>> LoadForItems(
        IEnumerable<string> itemRecordIds)
    {
        var result = new Dictionary<string, List<ItemVariantRecord>>(
            StringComparer.OrdinalIgnoreCase);
        foreach (var chunk in itemRecordIds.Distinct(StringComparer.OrdinalIgnoreCase).Chunk(400))
        {
            using var command = _connection.CreateCommand();
            var parameters = SqliteQuery.AddValues(command, "item", chunk);
            command.CommandText = $"""
                SELECT
                    IR.record_id,
                    AR.record_id,
                    COALESCE(AR.display_name, AR.record_id),
                    A.kind,
                    A.rarity,
                    A.item_level,
                    A.required_level,
                    A.jitter_percent,
                    COALESCE((
                        SELECT GROUP_CONCAT(S.record_id, ',')
                        FROM (
                            SELECT DISTINCT SR.record_id
                            FROM item_variants IV
                            JOIN records SR ON SR.id = IV.source_pk
                            WHERE IV.item_pk = V.item_pk AND IV.affix_pk = V.affix_pk
                            ORDER BY SR.record_id COLLATE NOCASE
                        ) S
                    ), '')
                FROM (SELECT DISTINCT item_pk, affix_pk FROM item_variants) V
                JOIN records IR ON IR.id = V.item_pk
                JOIN affixes A ON A.record_pk = V.affix_pk AND A.family = 'variant'
                JOIN records AR ON AR.id = A.record_pk
                WHERE IR.record_id IN ({parameters})
                ORDER BY IR.record_id COLLATE NOCASE, AR.record_id COLLATE NOCASE
                """;
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var itemRecordId = reader.GetString(0);
                if (!result.TryGetValue(itemRecordId, out var variants))
                {
                    variants = [];
                    result[itemRecordId] = variants;
                }
                variants.Add(new ItemVariantRecord
                {
                    RecordId = reader.GetString(1),
                    Name = reader.GetString(2),
                    Kind = reader.GetString(3),
                    Rarity = reader.GetString(4),
                    ItemLevel = reader.GetDouble(5),
                    RequiredLevel = reader.GetDouble(6),
                    JitterPercent = reader.GetDouble(7),
                    SourceRecordIds = _split(reader.GetString(8))
                });
            }
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
