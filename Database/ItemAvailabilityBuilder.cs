using Microsoft.Data.Sqlite;

namespace GdCli.Database;

internal static class ItemAvailabilityBuilder
{
    public static void Build(SqliteConnection connection, SqliteTransaction transaction)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            UPDATE items
            SET availability = CASE
                WHEN EXISTS (
                    SELECT 1
                    FROM acquisition_sources A
                    WHERE A.item_pk = items.record_pk
                ) OR EXISTS (
                    SELECT 1
                    FROM recipes R
                    WHERE R.result_item_pk = items.record_pk
                ) THEN 'known'
                WHEN EXISTS (
                    SELECT 1
                    FROM record_references RR
                    JOIN field_names N ON N.id = RR.field_pk
                    WHERE RR.target_pk = items.record_pk
                      AND RR.source_pk <> items.record_pk
                      AND N.name NOT IN ('setMembers', 'blacklistedSets')
                ) OR EXISTS (
                    SELECT 1
                    FROM placements P
                    WHERE P.record_pk = items.record_pk
                ) OR EXISTS (
                    SELECT 1
                    FROM quest_entities E
                    WHERE E.record_pk = items.record_pk
                ) OR EXISTS (
                    SELECT 1
                    FROM quest_actions A
                    JOIN records R ON R.record_id = A.record_id COLLATE NOCASE
                    WHERE R.id = items.record_pk
                ) OR EXISTS (
                    SELECT 1
                    FROM quest_conditions C
                    JOIN records R ON R.record_id = C.record_id COLLATE NOCASE
                    WHERE R.id = items.record_pk
                ) THEN 'referenced'
                ELSE 'unresolved'
            END;

            UPDATE items
            SET availability = 'unavailable'
            WHERE record_pk IN (
                SELECT M.item_pk
                FROM item_set_members M
                WHERE EXISTS (
                    SELECT 1
                    FROM record_references RR
                    JOIN field_names N ON N.id = RR.field_pk
                    WHERE RR.target_pk = M.set_pk
                      AND N.name = 'blacklistedSets'
                )
                  AND NOT EXISTS (
                      SELECT 1
                      FROM item_set_members SM
                      JOIN items SI ON SI.record_pk = SM.item_pk
                      WHERE SM.set_pk = M.set_pk
                        AND SI.availability <> 'unresolved'
                  )
                  AND NOT EXISTS (
                      SELECT 1
                      FROM record_references RR
                      JOIN field_names N ON N.id = RR.field_pk
                      WHERE RR.target_pk = M.set_pk
                        AND N.name NOT IN ('itemSetName', 'blacklistedSets')
                  )
            );

            UPDATE item_sets
            SET availability = CASE
                WHEN EXISTS (
                    SELECT 1
                    FROM item_set_members M
                    JOIN items I ON I.record_pk = M.item_pk
                    WHERE M.set_pk = item_sets.record_pk
                      AND I.availability = 'known'
                ) THEN 'known'
                WHEN EXISTS (
                    SELECT 1
                    FROM item_set_members M
                    JOIN items I ON I.record_pk = M.item_pk
                    WHERE M.set_pk = item_sets.record_pk
                      AND I.availability = 'referenced'
                ) THEN 'referenced'
                WHEN EXISTS (
                    SELECT 1
                    FROM item_set_members M
                    JOIN items I ON I.record_pk = M.item_pk
                    WHERE M.set_pk = item_sets.record_pk
                      AND I.availability = 'unresolved'
                ) THEN 'unresolved'
                ELSE 'unavailable'
            END;
            """;
        command.ExecuteNonQuery();
    }
}
