using Microsoft.Data.Sqlite;

namespace GdCli.Database;

internal static class ItemSetCatalogBuilder
{
    public static void Build(SqliteConnection connection, SqliteTransaction transaction)
    {
        _execute(connection, transaction, """
            INSERT INTO item_sets(record_pk, item_level)
            SELECT
                R.id,
                COALESCE((SELECT F.numeric_value
                          FROM record_fields F
                          JOIN field_names N ON N.id = F.field_pk
                          WHERE F.record_pk = R.id AND N.name = 'itemLevel'
                          LIMIT 1), 0)
            FROM records R
            WHERE R.template LIKE '%/itemset.tpl'
              AND EXISTS (
                  SELECT 1
                  FROM record_references RR
                  JOIN field_names N ON N.id = RR.field_pk
                  JOIN items I ON I.record_pk = RR.target_pk
                  WHERE RR.source_pk = R.id AND N.name = 'setMembers'
              );

            INSERT OR IGNORE INTO item_set_members(set_pk, item_pk, ordinal)
            SELECT RR.source_pk, RR.target_pk, RR.ordinal
            FROM record_references RR
            JOIN item_sets S ON S.record_pk = RR.source_pk
            JOIN items I ON I.record_pk = RR.target_pk
            JOIN field_names N ON N.id = RR.field_pk
            WHERE N.name = 'setMembers';
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
