using GdCli.Contracts;
using GdCli.Database;
using GdCli.Features.Affixes.Formatting;
using GdCli.Features.SkillModifiers;

namespace GdCli.Features.Affixes;

internal sealed class AffixDetailLoader
{
    private readonly CliDatabase _database;
    private readonly SkillModifierLoader _skillModifiers;

    public AffixDetailLoader(CliDatabase database)
    {
        _database = database;
        _skillModifiers = new SkillModifierLoader(database);
    }

    public void Populate(IReadOnlyList<AffixRecord> affixes)
    {
        if (affixes.Count == 0)
            return;

        var stats = _database.LoadStats(affixes.Select(affix => affix.RecordId));
        var modifiers = _skillModifiers.Load(affixes.Select(affix => affix.RecordId));
        var effectBuilder = _createEffectBuilder(stats.Values);
        foreach (var affix in affixes)
        {
            affix.Stats = stats.GetValueOrDefault(affix.RecordId) ?? [];
            var skillModifiers = modifiers.GetValueOrDefault(affix.RecordId);
            affix.SkillModifiers = skillModifiers is { Count: > 0 } ? skillModifiers : null;
            effectBuilder.Apply(affix);
        }
    }

    public void PopulateVariants(IReadOnlyList<ItemVariantRecord> variants)
    {
        if (variants.Count == 0)
            return;

        var stats = _database.LoadStats(variants.Select(variant => variant.RecordId));
        var modifiers = _skillModifiers.Load(variants.Select(variant => variant.RecordId));
        var effectBuilder = _createEffectBuilder(stats.Values);
        foreach (var variant in variants)
        {
            variant.Stats = stats.GetValueOrDefault(variant.RecordId) ?? [];
            var skillModifiers = modifiers.GetValueOrDefault(variant.RecordId);
            variant.SkillModifiers = skillModifiers is { Count: > 0 } ? skillModifiers : null;
            effectBuilder.Apply(variant);
        }
    }

    private AffixEffectBuilder _createEffectBuilder(IEnumerable<List<RawStat>> stats)
    {
        var skillRecords = stats
            .SelectMany(value => value)
            .Where(stat =>
                stat.Field.StartsWith("augmentSkillName", StringComparison.Ordinal) &&
                !string.IsNullOrWhiteSpace(stat.TextValue))
            .Select(stat => stat.TextValue ?? string.Empty);
        return new AffixEffectBuilder(
            new EnglishStatTags(_database.LoadTags()),
            _database.LoadRecordNames(skillRecords));
    }
}
