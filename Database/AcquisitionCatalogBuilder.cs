using Microsoft.Data.Sqlite;

namespace GdCli.Database;

internal static class AcquisitionCatalogBuilder
{
    public static void Build(SqliteConnection connection, SqliteTransaction transaction)
    {
        _buildRecipes(connection, transaction);
        _buildSpecificMonsterSources(connection, transaction);
        _buildVendorSources(connection, transaction);
        _buildRandomSources(connection, transaction);
        _updateMiFlags(connection, transaction);
        AcquisitionGraphPruner.Prune(connection, transaction);
    }

    private static void _buildRecipes(SqliteConnection connection, SqliteTransaction transaction)
    {
        _execute(connection, transaction, """
            WITH RECURSIVE RecipeGraph(recipe_pk, record_pk) AS (
                SELECT I.record_pk, RR.target_pk
                FROM items I
                JOIN record_references RR ON RR.source_pk = I.record_pk
                JOIN field_names N ON N.id = RR.field_pk
                WHERE N.name = 'artifactName'
                UNION
                SELECT G.recipe_pk, RR.target_pk
                FROM RecipeGraph G
                JOIN records S ON S.id = G.record_pk
                JOIN record_references RR ON RR.source_pk = G.record_pk
                JOIN field_names N ON N.id = RR.field_pk
                WHERE S.record_id LIKE 'records/items/loottables/%'
                  AND (N.name LIKE 'lootName%' OR N.name = 'records')
            )
            INSERT OR IGNORE INTO recipes(result_item_pk, recipe_item_pk)
            SELECT DISTINCT G.record_pk, G.recipe_pk
            FROM RecipeGraph G
            JOIN items I ON I.record_pk = G.record_pk
            WHERE G.record_pk <> G.recipe_pk;
            """);
    }

    private static void _buildSpecificMonsterSources(
        SqliteConnection connection,
        SqliteTransaction transaction)
    {
        _execute(connection, transaction, """
            WITH RECURSIVE SpecificLoot(item_pk, record_pk) AS (
                SELECT I.record_pk, I.record_pk
                FROM items I
                JOIN records R ON R.id = I.record_pk
                WHERE ((R.record_id LIKE 'records/items/gear%'
                        OR R.class LIKE 'Weapon%'
                        OR R.class LIKE 'Armor%')
                       AND I.rarity IN ('Rare', 'Epic', 'Legendary'))
                   OR I.item_class LIKE '%Formula'
                UNION
                SELECT G.item_pk, RR.source_pk
                FROM SpecificLoot G
                JOIN record_references RR ON RR.target_pk = G.record_pk
                JOIN records S ON S.id = RR.source_pk
                JOIN field_names N ON N.id = RR.field_pk
                WHERE (S.record_id LIKE 'records/items/loottables/%' AND N.name LIKE 'lootName%')
                   OR (S.record_id LIKE 'records/creatures/%' AND N.name LIKE 'loot%Item%')
            )
            INSERT OR IGNORE INTO acquisition_sources(item_pk, kind, source_pk)
            SELECT DISTINCT G.item_pk, 'specificMonster', G.record_pk
            FROM SpecificLoot G
            JOIN records R ON R.id = G.record_pk
            WHERE R.record_id LIKE 'records/creatures/%'
              AND (R.class = 'Monster' OR R.template LIKE '%/monster%.tpl');
            """);
    }

    private static void _buildVendorSources(SqliteConnection connection, SqliteTransaction transaction)
    {
        _execute(connection, transaction, """
            WITH RECURSIVE MerchantInventory(merchant_pk, record_pk) AS (
                SELECT M.id, RR.target_pk
                FROM records M
                JOIN record_references RR ON RR.source_pk = M.id
                JOIN field_names N ON N.id = RR.field_pk
                WHERE M.class = 'NpcMerchant'
                  AND N.name = 'marketFileName'
                UNION
                SELECT G.merchant_pk, RR.target_pk
                FROM MerchantInventory G
                JOIN records S ON S.id = G.record_pk
                JOIN record_references RR ON RR.source_pk = G.record_pk
                JOIN field_names N ON N.id = RR.field_pk
                WHERE (S.template LIKE '%/market.tpl' AND N.name LIKE 'market%Table')
                   OR (S.record_id LIKE 'records/items/loottables/%'
                       AND (N.name LIKE 'lootName%' OR N.name = 'records'))
            )
            INSERT OR IGNORE INTO acquisition_sources(item_pk, kind, source_pk)
            SELECT DISTINCT G.record_pk, 'vendor', G.merchant_pk
            FROM MerchantInventory G
            JOIN items I ON I.record_pk = G.record_pk;
            """);
    }

    private static void _buildRandomSources(SqliteConnection connection, SqliteTransaction transaction)
    {
        _execute(connection, transaction, """
            WITH RECURSIVE RandomLoot(record_pk) AS (
                SELECT id
                FROM records
                WHERE class = 'LootMasterTable'
                   OR template LIKE '%/lootmastertable.tpl'
                UNION
                SELECT RR.target_pk
                FROM RandomLoot G
                JOIN records S ON S.id = G.record_pk
                JOIN record_references RR ON RR.source_pk = G.record_pk
                JOIN field_names N ON N.id = RR.field_pk
                WHERE S.record_id LIKE 'records/items/loottables/%'
                  AND (N.name LIKE 'lootName%' OR N.name = 'records')
            )
            INSERT OR IGNORE INTO acquisition_sources(item_pk, kind, source_pk)
            SELECT DISTINCT G.record_pk, 'randomDrop', NULL
            FROM RandomLoot G
            JOIN items I ON I.record_pk = G.record_pk;
            """);
    }

    private static void _updateMiFlags(SqliteConnection connection, SqliteTransaction transaction)
    {
        _execute(connection, transaction, """
            UPDATE items
            SET is_mi = items.rarity IN ('Rare', 'Epic', 'Legendary')
                AND EXISTS (
                    SELECT 1
                    FROM records R
                    WHERE R.id = items.record_pk
                      AND (R.record_id LIKE 'records/items/gear%'
                           OR R.class LIKE 'Weapon%'
                           OR R.class LIKE 'Armor%')
                )
                AND EXISTS (
                    SELECT 1
                    FROM acquisition_sources A
                    WHERE A.item_pk = items.record_pk
                      AND A.kind = 'specificMonster'
                );
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
