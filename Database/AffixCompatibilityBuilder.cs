using Microsoft.Data.Sqlite;

namespace GdCli.Database;

internal static class AffixCompatibilityBuilder
{
    public static void Build(SqliteConnection connection, SqliteTransaction transaction)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            WITH RECURSIVE
            DynamicItemTables(item_class, table_pk, slot) AS (
                SELECT DISTINCT
                    I.item_class,
                    RR.source_pk,
                    substr(N.name, length('lootName') + 1)
                FROM record_references RR
                JOIN records S ON S.id = RR.source_pk
                JOIN field_names N ON N.id = RR.field_pk
                JOIN items I ON I.record_pk = RR.target_pk
                WHERE S.class = 'LootItemTable_DynWeight'
                  AND N.name LIKE 'lootName%'
                  AND I.item_class <> ''
            ),
            AffixSeeds(item_class, record_pk) AS (
                SELECT DISTINCT D.item_class, RR.target_pk
                FROM DynamicItemTables D
                JOIN record_references RR ON RR.source_pk = D.table_pk
                JOIN field_names N ON N.id = RR.field_pk
                WHERE N.name IN (
                    'prefixTableName' || D.slot,
                    'suffixTableName' || D.slot,
                    'rarePrefixTableName' || D.slot,
                    'rareSuffixTableName' || D.slot)
            ),
            AffixGraph(item_class, record_pk) AS (
                SELECT item_class, record_pk FROM AffixSeeds
                UNION
                SELECT G.item_class, RR.target_pk
                FROM AffixGraph G
                JOIN records S ON S.id = G.record_pk
                JOIN record_references RR ON RR.source_pk = G.record_pk
                JOIN field_names N ON N.id = RR.field_pk
                WHERE S.class = 'LootRandomizerTable'
                  AND N.name LIKE 'randomizerName%'
            )
            INSERT OR IGNORE INTO affix_item_classes(item_class, affix_pk)
            SELECT G.item_class, G.record_pk
            FROM AffixGraph G
            JOIN affixes A ON A.record_pk = G.record_pk AND A.family = 'standard';
            """;
        command.ExecuteNonQuery();
    }
}
