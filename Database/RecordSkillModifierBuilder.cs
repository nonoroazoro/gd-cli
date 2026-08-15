using Microsoft.Data.Sqlite;

namespace GdCli.Database;

internal static class RecordSkillModifierBuilder
{
    public static void Build(SqliteConnection connection, SqliteTransaction transaction)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT OR IGNORE INTO record_skill_modifiers(
                owner_pk, modifier_pk, ordinal, skill_pk)
            SELECT
                RR.source_pk,
                RR.target_pk,
                COALESCE(
                    NULLIF(CAST(substr(N.name, length('modifierSkillName') + 1) AS INTEGER), 0),
                    RR.ordinal + 1),
                SR.target_pk
            FROM record_references RR
            JOIN field_names N ON N.id = RR.field_pk
            LEFT JOIN item_sets S ON S.record_pk = RR.source_pk
            LEFT JOIN field_names SN
              ON SN.name = 'modifiedSkillName' || substr(N.name, length('modifierSkillName') + 1)
            LEFT JOIN record_references SR
              ON SR.source_pk = RR.source_pk
             AND SR.field_pk = SN.id
             AND SR.ordinal = RR.ordinal
            WHERE N.name LIKE 'modifierSkillName%'
              AND (EXISTS (SELECT 1 FROM items I WHERE I.record_pk = RR.source_pk)
                   OR S.record_pk IS NOT NULL
                   OR EXISTS (SELECT 1 FROM affixes A WHERE A.record_pk = RR.source_pk));
            """;
        command.ExecuteNonQuery();
    }
}
