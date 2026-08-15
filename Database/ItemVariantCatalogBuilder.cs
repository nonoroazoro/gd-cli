using Microsoft.Data.Sqlite;

namespace GdCli.Database;

internal static class ItemVariantCatalogBuilder
{
    public static void Build(SqliteConnection connection, SqliteTransaction transaction)
    {
        _execute(connection, transaction, """
            CREATE TEMP TABLE derived_item_variants (
                item_pk INTEGER NOT NULL,
                affix_pk INTEGER NOT NULL,
                source_pk INTEGER NOT NULL,
                PRIMARY KEY (item_pk, affix_pk, source_pk)
            ) WITHOUT ROWID;

            WITH RECURSIVE
            VariantSeeds(item_pk, source_pk, kind, record_pk) AS (
                SELECT DISTINCT
                    I.record_pk,
                    D.id,
                    CASE
                        WHEN N.name LIKE '%PrefixTableName%' THEN 'prefix'
                        ELSE 'suffix'
                    END,
                    RR.target_pk
                FROM records D
                JOIN record_references ItemReference ON ItemReference.source_pk = D.id
                JOIN field_names ItemField ON ItemField.id = ItemReference.field_pk
                JOIN items I ON I.record_pk = ItemReference.target_pk
                JOIN record_references RR ON RR.source_pk = D.id
                JOIN field_names N ON N.id = RR.field_pk
                WHERE D.class = 'LootItemTable_DynWeight'
                  AND ItemField.name LIKE 'lootName%'
                  AND N.name IN (
                      'prefixTableName' || substr(ItemField.name, length('lootName') + 1),
                      'suffixTableName' || substr(ItemField.name, length('lootName') + 1),
                      'rarePrefixTableName' || substr(ItemField.name, length('lootName') + 1),
                      'rareSuffixTableName' || substr(ItemField.name, length('lootName') + 1))
            ),
            VariantGraph(item_pk, source_pk, kind, record_pk) AS (
                SELECT item_pk, source_pk, kind, record_pk
                FROM VariantSeeds
                UNION
                SELECT G.item_pk, G.source_pk, G.kind, RR.target_pk
                FROM VariantGraph G
                JOIN records S ON S.id = G.record_pk
                JOIN record_references RR ON RR.source_pk = G.record_pk
                JOIN field_names N ON N.id = RR.field_pk
                WHERE S.class = 'LootRandomizerTable'
                  AND N.name LIKE 'randomizerName%'
            )
            INSERT OR IGNORE INTO derived_item_variants(item_pk, affix_pk, source_pk)
            SELECT G.item_pk, G.record_pk, G.source_pk
            FROM VariantGraph G
            JOIN records A ON A.id = G.record_pk
            WHERE A.class = 'LootRandomizer'
              AND (A.record_id LIKE '%/lootaffixes/prefixunique/%'
                   OR A.record_id LIKE '%/lootaffixes/suffixunique/%');

            INSERT OR IGNORE INTO affixes(
                record_pk, family, kind, rarity, item_level, required_level, jitter_percent)
            SELECT DISTINCT
                R.id,
                'variant',
                CASE WHEN R.record_id LIKE '%/prefixunique/%' THEN 'prefix' ELSE 'suffix' END,
                COALESCE((SELECT F.text_value FROM record_fields F JOIN field_names N ON N.id = F.field_pk
                          WHERE F.record_pk = R.id AND N.name = 'itemClassification' LIMIT 1), ''),
                COALESCE((SELECT F.numeric_value FROM record_fields F JOIN field_names N ON N.id = F.field_pk
                          WHERE F.record_pk = R.id AND N.name = 'itemLevel' LIMIT 1), 0),
                COALESCE((SELECT F.numeric_value FROM record_fields F JOIN field_names N ON N.id = F.field_pk
                          WHERE F.record_pk = R.id AND N.name = 'levelRequirement' LIMIT 1), 0),
                COALESCE((SELECT F.numeric_value FROM record_fields F JOIN field_names N ON N.id = F.field_pk
                          WHERE F.record_pk = R.id AND N.name = 'lootRandomizerJitter' LIMIT 1), 0)
            FROM derived_item_variants V
            JOIN records R ON R.id = V.affix_pk;

            INSERT OR IGNORE INTO item_variants(item_pk, affix_pk, source_pk)
            SELECT V.item_pk, V.affix_pk, V.source_pk
            FROM derived_item_variants V
            JOIN affixes A ON A.record_pk = V.affix_pk AND A.family = 'variant';

            DROP TABLE derived_item_variants;
            """);
    }

    private static void _execute(
        SqliteConnection connection,
        SqliteTransaction transaction,
        string sql)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }
}
