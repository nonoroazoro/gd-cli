using GdCli.Contracts;
using GdCli.Database;
using GdCli.Features.SkillModifiers;

namespace GdCli.Features.Items;

internal sealed class ItemSetBonusLoader
{
    private readonly CliDatabase _database;
    private readonly SkillModifierLoader _skillModifiers;

    public ItemSetBonusLoader(CliDatabase database)
    {
        _database = database;
        _skillModifiers = new SkillModifierLoader(database);
    }

    public void Populate(IReadOnlyList<ItemSetRecord> itemSets)
    {
        if (itemSets.Count == 0)
            return;

        var recordIds = itemSets.Select(itemSet => itemSet.RecordId).ToArray();
        var bonuses = _database.ItemSets.LoadBonuses(recordIds);
        var definitions = _database.ItemSets.LoadBonusDefinitions(recordIds);
        var modifiers = _skillModifiers.LoadSetBonuses(recordIds);
        foreach (var itemSet in itemSets)
        {
            var setBonuses = bonuses.GetValueOrDefault(itemSet.RecordId) ?? [];
            _addDefinitions(setBonuses, definitions.GetValueOrDefault(itemSet.RecordId) ?? []);
            _addModifiers(setBonuses, modifiers.GetValueOrDefault(itemSet.RecordId) ?? []);
            itemSet.Bonuses = setBonuses;
        }
    }

    private static void _addDefinitions(
        IEnumerable<ItemSetBonus> bonuses,
        IReadOnlyList<RawStat> definitions)
    {
        var byField = definitions.ToDictionary(stat => stat.Field, StringComparer.Ordinal);
        foreach (var bonus in bonuses)
        {
            var requiredFields = bonus.Stats
                .SelectMany(stat => _definitionFields(stat.Field))
                .Distinct(StringComparer.Ordinal)
                .ToArray();
            foreach (var field in requiredFields)
            {
                if (byField.TryGetValue(field, out var definition))
                    bonus.Stats.Add(definition);
            }
            bonus.Stats.Sort((left, right) => string.CompareOrdinal(left.Field, right.Field));
        }
    }

    private static void _addModifiers(
        List<ItemSetBonus> bonuses,
        IReadOnlyDictionary<int, List<SkillModifier>> modifiers)
    {
        foreach (var group in modifiers)
        {
            var bonus = bonuses.FirstOrDefault(value => value.RequiredPieces == group.Key);
            if (bonus == null)
            {
                bonus = new ItemSetBonus { RequiredPieces = group.Key };
                bonuses.Add(bonus);
            }
            bonus.SkillModifiers = group.Value;
        }
        bonuses.Sort((left, right) => left.RequiredPieces.CompareTo(right.RequiredPieces));
    }

    private static IEnumerable<string> _definitionFields(string field)
    {
        if (field.StartsWith("augmentSkillLevel", StringComparison.Ordinal))
        {
            yield return $"augmentSkillName{field["augmentSkillLevel".Length..]}";
            yield break;
        }
        if (field is "itemSkillLevel" or "itemSkillLevelEq")
        {
            yield return "itemSkillName";
            yield return "itemSkillAutoController";
        }
    }
}
