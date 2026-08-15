using GdCli.Contracts;
using Microsoft.Data.Sqlite;

namespace GdCli.Database;

internal sealed class RecordSkillModifierRepository
{
    private readonly SqliteConnection _connection;

    public RecordSkillModifierRepository(SqliteConnection connection)
    {
        _connection = connection;
    }

    public Dictionary<string, List<SkillModifier>> Load(IEnumerable<string> ownerRecordIds)
    {
        var result = new Dictionary<string, List<SkillModifier>>(StringComparer.OrdinalIgnoreCase);
        foreach (var chunk in ownerRecordIds.Distinct(StringComparer.OrdinalIgnoreCase).Chunk(400))
            _loadChunk(chunk, result);
        return result;
    }

    public Dictionary<string, Dictionary<int, List<SkillModifier>>> LoadSetBonuses(
        IEnumerable<string> setRecordIds)
    {
        var result = new Dictionary<string, Dictionary<int, List<SkillModifier>>>(StringComparer.OrdinalIgnoreCase);
        foreach (var chunk in setRecordIds.Distinct(StringComparer.OrdinalIgnoreCase).Chunk(400))
        {
            using var command = _connection.CreateCommand();
            var parameters = SqliteQuery.AddValues(command, "record", chunk);
            command.CommandText = $"""
                SELECT O.record_id, B.required_pieces, RSM.ordinal,
                       M.record_id, COALESCE(M.display_name, M.record_id),
                       S.record_id, S.display_name
                FROM item_set_bonuses B
                JOIN records O ON O.id = B.set_pk
                JOIN record_skill_modifiers RSM ON RSM.owner_pk = B.set_pk
                JOIN records M ON M.id = RSM.modifier_pk
                LEFT JOIN records S ON S.id = RSM.skill_pk
                WHERE B.has_skill_modifiers = 1
                  AND O.record_id IN ({parameters})
                ORDER BY O.record_id, B.required_pieces, RSM.ordinal, M.record_id
                """;
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var ownerRecordId = reader.GetString(0);
                if (!result.TryGetValue(ownerRecordId, out var bonuses))
                {
                    bonuses = [];
                    result[ownerRecordId] = bonuses;
                }
                var requiredPieces = reader.GetInt32(1);
                if (!bonuses.TryGetValue(requiredPieces, out var modifiers))
                {
                    modifiers = [];
                    bonuses[requiredPieces] = modifiers;
                }
                modifiers.Add(_readModifier(reader, 2));
            }
        }
        return result;
    }

    private void _loadChunk(
        string[] ownerRecordIds,
        Dictionary<string, List<SkillModifier>> result)
    {
        using var command = _connection.CreateCommand();
        var parameters = SqliteQuery.AddValues(command, "record", ownerRecordIds);
        command.CommandText = $"""
            SELECT O.record_id, RSM.ordinal, M.record_id, COALESCE(M.display_name, M.record_id),
                   S.record_id, S.display_name
            FROM record_skill_modifiers RSM
            JOIN records O ON O.id = RSM.owner_pk
            JOIN records M ON M.id = RSM.modifier_pk
            LEFT JOIN records S ON S.id = RSM.skill_pk
            WHERE O.record_id IN ({parameters})
            ORDER BY O.record_id, RSM.ordinal, M.record_id
            """;
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var ownerRecordId = reader.GetString(0);
            if (!result.TryGetValue(ownerRecordId, out var modifiers))
            {
                modifiers = [];
                result[ownerRecordId] = modifiers;
            }
            modifiers.Add(_readModifier(reader, 1));
        }
    }

    private static SkillModifier _readModifier(SqliteDataReader reader, int offset)
    {
        var skillRecordId = reader.IsDBNull(offset + 3) ? null : reader.GetString(offset + 3);
        var skillName = reader.IsDBNull(offset + 4) ? null : reader.GetString(offset + 4);
        return new SkillModifier
        {
            Ordinal = reader.GetInt32(offset),
            RecordId = reader.GetString(offset + 1),
            Name = reader.GetString(offset + 2),
            SkillRecordId = skillRecordId,
            SkillName = string.Equals(skillRecordId, skillName, StringComparison.OrdinalIgnoreCase)
                ? null
                : skillName
        };
    }
}
