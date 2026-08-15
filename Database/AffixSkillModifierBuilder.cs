using Microsoft.Data.Sqlite;

namespace GdCli.Database;

internal static class AffixSkillModifierBuilder
{
    public static void Build(SqliteConnection connection, SqliteTransaction transaction)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT OR IGNORE INTO affix_skill_modifiers(affix_pk, modifier_pk, ordinal, skill_pk)
            SELECT
                RR.source_pk,
                RR.target_pk,
                COALESCE(
                    NULLIF(CAST(substr(N.name, length('modifierSkillName') + 1) AS INTEGER), 0),
                    RR.ordinal + 1),
                SR.target_pk
            FROM record_references RR
            JOIN affixes A ON A.record_pk = RR.source_pk
            JOIN field_names N ON N.id = RR.field_pk
            LEFT JOIN field_names SN
              ON SN.name = 'modifiedSkillName' || substr(N.name, length('modifierSkillName') + 1)
            LEFT JOIN record_references SR
              ON SR.source_pk = RR.source_pk AND SR.field_pk = SN.id
            WHERE A.family IN ('ascended', 'variant')
              AND N.name LIKE 'modifierSkillName%';
            """;
        command.ExecuteNonQuery();
    }
}
