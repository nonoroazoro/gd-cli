using Microsoft.Data.Sqlite;

namespace GdCli.Database;

internal static class AcquisitionGraphPruner
{
    public static void Prune(SqliteConnection connection, SqliteTransaction transaction)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            CREATE TEMP TABLE kept_edges (
                source_pk INTEGER NOT NULL,
                field_pk INTEGER NOT NULL,
                ordinal INTEGER NOT NULL,
                PRIMARY KEY (source_pk, field_pk, ordinal)
            ) WITHOUT ROWID;

            WITH RECURSIVE ItemRoutes(record_pk) AS (
                SELECT item_pk
                FROM acquisition_sources
                WHERE kind = 'specificMonster'
                UNION
                SELECT RR.source_pk
                FROM ItemRoutes R
                JOIN record_references RR ON RR.target_pk = R.record_pk
                JOIN records S ON S.id = RR.source_pk
                JOIN field_names N ON N.id = RR.field_pk
                WHERE (S.record_id LIKE 'records/items/loottables/%' AND N.name LIKE 'lootName%')
                   OR (S.record_id LIKE 'records/creatures/%' AND N.name LIKE 'loot%Item%')
            )
            INSERT OR IGNORE INTO kept_edges(source_pk, field_pk, ordinal)
            SELECT RR.source_pk, RR.field_pk, RR.ordinal
            FROM ItemRoutes R
            JOIN record_references RR ON RR.target_pk = R.record_pk
            JOIN records S ON S.id = RR.source_pk
            JOIN field_names N ON N.id = RR.field_pk
            WHERE (S.record_id LIKE 'records/items/loottables/%' AND N.name LIKE 'lootName%')
               OR (S.record_id LIKE 'records/creatures/%' AND N.name LIKE 'loot%Item%');

            WITH RECURSIVE LocationRoutes(record_pk) AS (
                SELECT source_pk
                FROM acquisition_sources
                WHERE kind IN ('specificMonster', 'vendor')
                UNION
                SELECT RR.source_pk
                FROM LocationRoutes R
                JOIN record_references RR ON RR.target_pk = R.record_pk
                JOIN records S ON S.id = RR.source_pk
                WHERE S.record_id LIKE 'records/proxies/%'
                   OR S.record_id LIKE 'records/creatures/%'
            )
            INSERT OR IGNORE INTO kept_edges(source_pk, field_pk, ordinal)
            SELECT RR.source_pk, RR.field_pk, RR.ordinal
            FROM LocationRoutes R
            JOIN record_references RR ON RR.target_pk = R.record_pk
            JOIN records S ON S.id = RR.source_pk
            WHERE S.record_id LIKE 'records/proxies/%'
               OR S.record_id LIKE 'records/creatures/%';

            DELETE FROM record_references
            WHERE NOT EXISTS (
                SELECT 1
                FROM kept_edges K
                WHERE K.source_pk = record_references.source_pk
                  AND K.field_pk = record_references.field_pk
                  AND K.ordinal = record_references.ordinal
            );

            DELETE FROM loot_conditions
            WHERE NOT EXISTS (
                SELECT 1
                FROM kept_edges K
                WHERE K.source_pk = loot_conditions.record_pk
            );

            DROP TABLE kept_edges;
            """;
        command.ExecuteNonQuery();
    }
}
