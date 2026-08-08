using Microsoft.Data.Sqlite;

namespace GdCli.Database;

internal static class MonsterDropBuilder
{
    public static void Build(SqliteConnection connection, SqliteTransaction transaction)
    {
        _execute(connection, transaction, """
            WITH RECURSIVE DropGraph(item_pk, record_pk) AS (
                SELECT I.record_pk, I.record_pk
                FROM items I
                JOIN records IR ON IR.id = I.record_pk
                WHERE (
                      IR.record_id LIKE 'records/items/gear%'
                      OR IR.class LIKE 'Weapon%'
                      OR IR.class LIKE 'Armor%'
                  )
                  AND I.rarity IN ('Rare', 'Epic', 'Legendary')
                UNION
                SELECT G.item_pk, RR.source_pk
                FROM DropGraph G
                JOIN record_references RR ON RR.target_pk = G.record_pk
                JOIN records S ON S.id = RR.source_pk
                JOIN field_names N ON N.id = RR.field_pk
                WHERE (
                      (S.record_id LIKE 'records/items/loottables/%' AND N.name LIKE 'lootName%')
                      OR (S.record_id LIKE 'records/creatures/%' AND N.name LIKE 'loot%Item%')
                  )
            )
            INSERT OR IGNORE INTO monster_drops(item_pk, monster_pk)
            SELECT G.item_pk, G.record_pk
            FROM DropGraph G
            JOIN records R ON R.id = G.record_pk
            WHERE R.record_id LIKE 'records/creatures/%'
              AND (R.class = 'Monster' OR R.template LIKE '%/monster%.tpl');

            UPDATE items
            SET is_mi = EXISTS (
                SELECT 1
                FROM monster_drops MD
                WHERE MD.item_pk = items.record_pk
            );
            """);

        _execute(connection, transaction, """
            CREATE TEMP TABLE kept_edges (
                source_pk INTEGER NOT NULL,
                field_pk INTEGER NOT NULL,
                ordinal INTEGER NOT NULL,
                PRIMARY KEY (source_pk, field_pk, ordinal)
            ) WITHOUT ROWID;

            WITH RECURSIVE ItemRoutes(record_pk) AS (
                SELECT item_pk FROM monster_drops
                UNION
                SELECT RR.source_pk
                FROM ItemRoutes R
                JOIN record_references RR ON RR.target_pk = R.record_pk
                JOIN records S ON S.id = RR.source_pk
                JOIN field_names N ON N.id = RR.field_pk
                WHERE (
                      (S.record_id LIKE 'records/items/loottables/%' AND N.name LIKE 'lootName%')
                      OR (S.record_id LIKE 'records/creatures/%' AND N.name LIKE 'loot%Item%')
                  )
            )
            INSERT OR IGNORE INTO kept_edges(source_pk, field_pk, ordinal)
            SELECT RR.source_pk, RR.field_pk, RR.ordinal
            FROM ItemRoutes R
            JOIN record_references RR ON RR.target_pk = R.record_pk
            JOIN records S ON S.id = RR.source_pk
            JOIN field_names N ON N.id = RR.field_pk
            WHERE (
                  (S.record_id LIKE 'records/items/loottables/%' AND N.name LIKE 'lootName%')
                  OR (S.record_id LIKE 'records/creatures/%' AND N.name LIKE 'loot%Item%')
              );

            WITH RECURSIVE LocationRoutes(record_pk) AS (
                SELECT monster_pk FROM monster_drops
                UNION
                SELECT RR.source_pk
                FROM LocationRoutes R
                JOIN record_references RR ON RR.target_pk = R.record_pk
                JOIN records S ON S.id = RR.source_pk
                WHERE S.record_id LIKE 'records/proxies/%' OR S.record_id LIKE 'records/creatures/%'
            )
            INSERT OR IGNORE INTO kept_edges(source_pk, field_pk, ordinal)
            SELECT RR.source_pk, RR.field_pk, RR.ordinal
            FROM LocationRoutes R
            JOIN record_references RR ON RR.target_pk = R.record_pk
            JOIN records S ON S.id = RR.source_pk
            WHERE S.record_id LIKE 'records/proxies/%' OR S.record_id LIKE 'records/creatures/%';

            DELETE FROM record_references
            WHERE NOT EXISTS (
                SELECT 1 FROM kept_edges K
                WHERE K.source_pk = record_references.source_pk
                  AND K.field_pk = record_references.field_pk
                  AND K.ordinal = record_references.ordinal
            );

            DELETE FROM drop_conditions
            WHERE NOT EXISTS (
                SELECT 1 FROM kept_edges K WHERE K.source_pk = drop_conditions.record_pk
            );

            DROP TABLE kept_edges;
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
