using Microsoft.Data.Sqlite;

namespace GdCli.Database;

internal static class SkillModifierFieldPruner
{
    public static void Prune(SqliteConnection connection, SqliteTransaction transaction)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            DELETE FROM record_fields
            WHERE record_pk IN (
                SELECT R.id
                FROM records R
                WHERE R.record_id LIKE '%/skillmodifiers/%'
            )
              AND record_pk NOT IN (
                  SELECT modifier_pk
                  FROM affix_skill_modifiers
              );
            """;
        command.ExecuteNonQuery();
    }
}
