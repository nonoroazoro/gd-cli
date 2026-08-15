using GdCli.Contracts;
using Microsoft.Data.Sqlite;

namespace GdCli.Database;

internal sealed class AffixSkillModifierRepository
{
    private readonly SqliteConnection _connection;

    public AffixSkillModifierRepository(SqliteConnection connection)
    {
        _connection = connection;
    }

    public Dictionary<string, List<AffixSkillModifier>> Load(IEnumerable<string> affixRecordIds)
    {
        var result = new Dictionary<string, List<AffixSkillModifier>>(StringComparer.OrdinalIgnoreCase);
        foreach (var chunk in affixRecordIds.Distinct(StringComparer.OrdinalIgnoreCase).Chunk(400))
        {
            using var command = _connection.CreateCommand();
            var parameters = SqliteQuery.AddValues(command, "record", chunk);
            command.CommandText = $"""
                SELECT A.record_id, ASM.ordinal, M.record_id, COALESCE(M.display_name, M.record_id),
                       S.record_id, S.display_name
                FROM affix_skill_modifiers ASM
                JOIN records A ON A.id = ASM.affix_pk
                JOIN records M ON M.id = ASM.modifier_pk
                LEFT JOIN records S ON S.id = ASM.skill_pk
                WHERE A.record_id IN ({parameters})
                ORDER BY A.record_id, ASM.ordinal, M.record_id
                """;
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var affixRecordId = reader.GetString(0);
                if (!result.TryGetValue(affixRecordId, out var modifiers))
                {
                    modifiers = [];
                    result[affixRecordId] = modifiers;
                }
                var skillRecordId = reader.IsDBNull(4) ? null : reader.GetString(4);
                var skillName = reader.IsDBNull(5) ? null : reader.GetString(5);
                modifiers.Add(new AffixSkillModifier
                {
                    Ordinal = reader.GetInt32(1),
                    RecordId = reader.GetString(2),
                    Name = reader.GetString(3),
                    SkillRecordId = skillRecordId,
                    SkillName = string.Equals(skillRecordId, skillName, StringComparison.OrdinalIgnoreCase)
                        ? null
                        : skillName
                });
            }
        }
        return result;
    }
}
