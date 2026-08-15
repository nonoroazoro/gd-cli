using GdCli.Contracts;
using GdCli.Database;

namespace GdCli.Features.SkillModifiers;

internal sealed class SkillModifierLoader
{
    private readonly CliDatabase _database;

    public SkillModifierLoader(CliDatabase database)
    {
        _database = database;
    }

    public Dictionary<string, List<SkillModifier>> Load(IEnumerable<string> ownerRecordIds)
    {
        var modifiers = _database.RecordSkillModifiers.Load(ownerRecordIds);
        _populateStats(modifiers.Values.SelectMany(value => value));
        return modifiers;
    }

    public Dictionary<string, Dictionary<int, List<SkillModifier>>> LoadSetBonuses(
        IEnumerable<string> setRecordIds)
    {
        var modifiers = _database.RecordSkillModifiers.LoadSetBonuses(setRecordIds);
        _populateStats(modifiers.Values
            .SelectMany(value => value.Values)
            .SelectMany(value => value));
        return modifiers;
    }

    private void _populateStats(IEnumerable<SkillModifier> modifiers)
    {
        var materialized = modifiers.ToList();
        var stats = _database.LoadStats(materialized.Select(modifier => modifier.RecordId));
        foreach (var modifier in materialized)
            modifier.Stats = stats.GetValueOrDefault(modifier.RecordId) ?? [];
    }
}
