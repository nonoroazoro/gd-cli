using Microsoft.Data.Sqlite;

namespace GdCli.Database;

internal static class AscendedAffixBuilder
{
    public static void Build(SqliteConnection connection, SqliteTransaction transaction)
    {
        _execute(connection, transaction, """
            INSERT INTO affixes(
                record_pk, family, kind, rarity, item_level, required_level, jitter_percent)
            SELECT
                R.id,
                'ascended',
                NULL,
                COALESCE((SELECT F.text_value FROM record_fields F JOIN field_names N ON N.id = F.field_pk
                          WHERE F.record_pk = R.id AND N.name = 'itemClassification' LIMIT 1), ''),
                COALESCE((SELECT F.numeric_value FROM record_fields F JOIN field_names N ON N.id = F.field_pk
                          WHERE F.record_pk = R.id AND N.name = 'itemLevel' LIMIT 1), 0),
                COALESCE((SELECT F.numeric_value FROM record_fields F JOIN field_names N ON N.id = F.field_pk
                          WHERE F.record_pk = R.id AND N.name = 'levelRequirement' LIMIT 1), 0),
                COALESCE((SELECT F.numeric_value FROM record_fields F JOIN field_names N ON N.id = F.field_pk
                          WHERE F.record_pk = R.id AND N.name = 'lootRandomizerJitter' LIMIT 1), 0)
            FROM records R
            WHERE R.class = 'LootRandomizer'
              AND R.record_id LIKE 'records/items/lootaffixes/ascended/%';
            """);

        _execute(connection, transaction, """
            WITH RECURSIVE
            FormulaSeeds(category, group_name, record_pk) AS (
                SELECT DISTINCT
                    CASE
                        WHEN N.name LIKE 'accessoryTables%' THEN 'accessory'
                        WHEN N.name LIKE 'armorTables%' THEN 'armor'
                        WHEN N.name LIKE 'offhandTables%' THEN 'offhand'
                        WHEN N.name LIKE 'oneHandMeleeTables%' THEN 'oneHandMelee'
                        WHEN N.name LIKE 'oneHandRangedTables%' THEN 'oneHandRanged'
                        WHEN N.name LIKE 'shieldTables%' THEN 'shield'
                        WHEN N.name LIKE 'twoHandMeleeTables%' THEN 'twoHandMelee'
                        WHEN N.name LIKE 'twoHandRangedTables%' THEN 'twoHandRanged'
                    END,
                    CASE WHEN N.name LIKE '%TablesMastery%' THEN 'mastery' ELSE 'affix' END,
                    RR.target_pk
                FROM record_references RR
                JOIN records F ON F.id = RR.source_pk
                JOIN field_names N ON N.id = RR.field_pk
                WHERE F.class = 'ItemAscensionFormula'
                  AND (N.name LIKE '%TablesAffix%' OR N.name LIKE '%TablesMastery%')
            ),
            AscendedGraph(category, group_name, record_pk) AS (
                SELECT category, group_name, record_pk
                FROM FormulaSeeds
                WHERE category IS NOT NULL
                UNION
                SELECT G.category, G.group_name, RR.target_pk
                FROM AscendedGraph G
                JOIN records S ON S.id = G.record_pk
                JOIN record_references RR ON RR.source_pk = G.record_pk
                JOIN field_names N ON N.id = RR.field_pk
                WHERE S.class = 'LootRandomizerTable'
                  AND N.name LIKE 'randomizerName%'
            )
            INSERT OR IGNORE INTO ascended_affix_categories(affix_pk, category, group_name)
            SELECT G.record_pk, G.category, G.group_name
            FROM AscendedGraph G
            JOIN affixes A ON A.record_pk = G.record_pk AND A.family = 'ascended';
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
