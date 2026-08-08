using Microsoft.Data.Sqlite;

namespace GdCli.Database;

internal static class GameRecordCatalogBuilder
{
    public static void Build(SqliteConnection connection, SqliteTransaction transaction)
    {
        _execute(connection, transaction, """
            UPDATE records
            SET display_name = COALESCE(
                (SELECT T.text FROM tags T WHERE T.tag = records.name_tag),
                (SELECT T.text FROM item_fields F
                 JOIN field_names N ON N.id = F.field_pk
                 JOIN tags T ON T.tag = F.text_value
                 WHERE F.record_pk = records.id
                   AND N.name IN ('itemNameTag', 'lootRandomizerName', 'description', 'skillDisplayName', 'artifactName')
                 ORDER BY CASE N.name
                     WHEN 'itemNameTag' THEN 1
                     WHEN 'lootRandomizerName' THEN 2
                     WHEN 'description' THEN 3
                     WHEN 'skillDisplayName' THEN 4
                     ELSE 5 END
                 LIMIT 1),
                NULLIF(name_tag, ''),
                (SELECT F.text_value FROM item_fields F
                 JOIN field_names N ON N.id = F.field_pk
                 WHERE F.record_pk = records.id AND N.name = 'FileDescription' LIMIT 1),
                record_id)
            """);

        _execute(connection, transaction, """
            INSERT INTO items(record_pk, name, rarity, item_class, item_level, required_level)
            SELECT
                R.id,
                R.display_name,
                COALESCE((SELECT F.text_value FROM item_fields F JOIN field_names N ON N.id = F.field_pk
                          WHERE F.record_pk = R.id AND N.name = 'itemClassification' LIMIT 1), ''),
                COALESCE(R.class, ''),
                COALESCE((SELECT F.numeric_value FROM item_fields F JOIN field_names N ON N.id = F.field_pk
                          WHERE F.record_pk = R.id AND N.name = 'itemLevel' LIMIT 1), 0),
                COALESCE((SELECT F.numeric_value FROM item_fields F JOIN field_names N ON N.id = F.field_pk
                          WHERE F.record_pk = R.id AND N.name = 'levelRequirement' LIMIT 1), 0)
            FROM records R
            WHERE R.record_id LIKE 'records/items/%'
              AND R.record_id NOT LIKE '%/loottables/%'
              AND R.record_id NOT LIKE '%/lootaffixes/%'
              AND (
                  R.class LIKE 'Item%'
                  OR R.template LIKE '%/item%.tpl'
                  OR EXISTS (SELECT 1 FROM item_fields F JOIN field_names N ON N.id = F.field_pk
                             WHERE F.record_pk = R.id AND N.name = 'itemNameTag')
              )
            """);

        _execute(connection, transaction, """
            INSERT INTO affixes(record_pk, name, kind, rarity, item_level, required_level, jitter_percent)
            SELECT
                R.id,
                R.display_name,
                CASE WHEN R.record_id LIKE '%/prefix/%' THEN 'prefix' ELSE 'suffix' END,
                COALESCE((SELECT F.text_value FROM item_fields F JOIN field_names N ON N.id = F.field_pk
                          WHERE F.record_pk = R.id AND N.name = 'itemClassification' LIMIT 1), ''),
                COALESCE((SELECT F.numeric_value FROM item_fields F JOIN field_names N ON N.id = F.field_pk
                          WHERE F.record_pk = R.id AND N.name = 'itemLevel' LIMIT 1), 0),
                COALESCE((SELECT F.numeric_value FROM item_fields F JOIN field_names N ON N.id = F.field_pk
                          WHERE F.record_pk = R.id AND N.name = 'levelRequirement' LIMIT 1), 0),
                COALESCE((SELECT F.numeric_value FROM item_fields F JOIN field_names N ON N.id = F.field_pk
                          WHERE F.record_pk = R.id AND N.name = 'lootRandomizerJitter' LIMIT 1), 0)
            FROM records R
            WHERE R.class = 'LootRandomizer'
              AND (
                  R.record_id LIKE '%/lootaffixes/prefix/%'
                  OR R.record_id LIKE '%/lootaffixes/suffix/%'
              )
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
