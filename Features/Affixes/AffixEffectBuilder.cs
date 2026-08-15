using System.Text.RegularExpressions;
using GdCli.Contracts;
using GdCli.Features.Affixes.Engine;
using GdCli.Features.Affixes.Formatting;

namespace GdCli.Features.Affixes;

internal sealed partial class AffixEffectBuilder
{
    private readonly StatFormatter _formatter;
    private readonly IReadOnlyDictionary<string, string> _skillNames;

    public AffixEffectBuilder(
        IStatTagProvider statTags,
        IReadOnlyDictionary<string, string>? skillNames = null)
    {
        _formatter = new StatFormatter(statTags);
        _skillNames = skillNames ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }

    public void Apply(AffixRecord affix)
    {
        var stats = affix.Stats ?? throw new InvalidOperationException("Affix stats must be loaded before effect calculation.");
        var skillBonuses = _buildSkillBonuses(stats);
        affix.SkillBonuses = skillBonuses;
        var result = _calculate(
            stats,
            affix.Family == "ascended" ||
            string.Equals(affix.Kind, "prefix", StringComparison.OrdinalIgnoreCase),
            skillBonuses ?? []);
        affix.JitterPercent = result.JitterPercent;
        affix.UnmodeledFields = result.UnmodeledFields;
        affix.Effects = result.Effects;
    }

    public void Apply(ItemVariantRecord variant)
    {
        var stats = variant.Stats ?? throw new InvalidOperationException(
            "Variant stats must be loaded before effect calculation.");
        var skillBonuses = _buildSkillBonuses(stats);
        variant.SkillBonuses = skillBonuses;
        var result = _calculate(
            stats,
            variant.Kind.Equals("prefix", StringComparison.OrdinalIgnoreCase),
            skillBonuses ?? []);
        variant.JitterPercent = result.JitterPercent;
        variant.UnmodeledFields = result.UnmodeledFields;
        variant.Effects = result.Effects;
    }

    private (
        double JitterPercent,
        IReadOnlyList<StatEffect> Effects,
        IReadOnlyList<string> UnmodeledFields) _calculate(
            IReadOnlyList<RawStat> stats,
            bool usePrefixStore,
            IReadOnlyList<SkillBonus> skillBonuses)
    {
        var input = stats
            .Where(stat => !string.IsNullOrWhiteSpace(stat.Field))
            .GroupBy(stat => stat.Field, StringComparer.Ordinal)
            .Select(group => group.OrderByDescending(stat => stat.Value).First())
            .Select(stat => new StatInput(stat.Field, stat.TextValue ?? string.Empty, stat.Value))
            .ToList();

        var range = usePrefixStore
            ? ItemStatEngine.ComputeRange([], prefixStats: input)
            : ItemStatEngine.ComputeRange([], suffixStats: input);

        var minimum = _extract(range.Minimum);
        var maximum = _extract(range.Maximum);
        foreach (var stat in stats.Where(stat => stat.TextValue == null))
        {
            stat.Minimum = minimum.TryGetValue(stat.Field, out var minimumValue) ? minimumValue : stat.Value;
            stat.Maximum = maximum.TryGetValue(stat.Field, out var maximumValue) ? maximumValue : stat.Value;
        }

        var jitterPercent = stats
            .FirstOrDefault(stat => stat.Field == "lootRandomizerJitter")?.Value ?? 0;
        var unmodeledFields = range.Minimum.UnmodeledFields
            .Concat(range.Maximum.UnmodeledFields)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToList();
        return (
            jitterPercent,
            _buildEffects(stats, minimum, maximum, skillBonuses),
            unmodeledFields);
    }

    private List<StatEffect> _buildEffects(
        IReadOnlyList<RawStat> raw,
        IReadOnlyDictionary<string, double> minimum,
        IReadOnlyDictionary<string, double> maximum,
        IReadOnlyList<SkillBonus> skillBonuses)
    {
        var effects = new List<StatEffect>();
        _addSection(effects, "header", raw, minimum, maximum, skillBonuses, StatSection.Header);
        _addSection(effects, "body", raw, minimum, maximum, skillBonuses, StatSection.Body);
        _addSection(effects, "pet", raw, minimum, maximum, skillBonuses, StatSection.Pet);
        return effects;
    }

    private void _addSection(
        List<StatEffect> output,
        string section,
        IReadOnlyList<RawStat> raw,
        IReadOnlyDictionary<string, double> minimum,
        IReadOnlyDictionary<string, double> maximum,
        IReadOnlyList<SkillBonus> skillBonuses,
        StatSection type)
    {
        var minimumText = _formatter.ProcessStats(_buildStats(raw, minimum, skillBonuses), type)
            .Select(stat => stat.ToString())
            .Where(text => !string.IsNullOrWhiteSpace(text))
            .ToList();
        var maximumBuckets = _formatter.ProcessStats(_buildStats(raw, maximum, skillBonuses), type)
            .Select(stat => stat.ToString())
            .Where(text => !string.IsNullOrWhiteSpace(text))
            .GroupBy(_normalize)
            .ToDictionary(
                group => group.Key,
                group => new Queue<string>(group),
                StringComparer.Ordinal);

        foreach (var minimumLine in minimumText)
        {
            var key = _normalize(minimumLine);
            var maximumLine = maximumBuckets.TryGetValue(key, out var bucket) && bucket.Count > 0
                ? bucket.Dequeue()
                : minimumLine;
            output.Add(new StatEffect
            {
                Section = section,
                Minimum = minimumLine,
                Maximum = maximumLine
            });
        }
    }

    private static HashSet<StatValue> _buildStats(
        IReadOnlyList<RawStat> raw,
        IReadOnlyDictionary<string, double> values,
        IReadOnlyList<SkillBonus> skillBonuses)
    {
        var result = raw
            .Where(stat => stat.TextValue != null)
            .GroupBy(stat => stat.Field, StringComparer.Ordinal)
            .Select(group => group.First())
            .Select(stat => new StatValue
            {
                Field = stat.Field,
                TextValue = stat.TextValue,
                Value = (float)stat.Value
            })
            .ToHashSet();

        for (var index = 0; index < skillBonuses.Count; index++)
        {
            var skillBonus = skillBonuses[index];
            result.Add(new StatValue
            {
                Field = $"augmentSkill{index + 1}",
                TextValue = skillBonus.Name ?? skillBonus.RecordId,
                Value = (float)skillBonus.Level
            });
        }

        foreach (var entry in values)
        {
            result.Add(new StatValue
            {
                Field = entry.Key,
                Value = (float)entry.Value
            });
        }

        return result;
    }

    private List<SkillBonus>? _buildSkillBonuses(IReadOnlyList<RawStat> stats)
    {
        var result = new List<SkillBonus>();
        foreach (var skillName in stats
                     .Where(stat =>
                         stat.Field.StartsWith("augmentSkillName", StringComparison.Ordinal) &&
                         !string.IsNullOrWhiteSpace(stat.TextValue))
                     .OrderBy(stat => stat.Field, StringComparer.Ordinal))
        {
            var slot = skillName.Field["augmentSkillName".Length..];
            var level = stats.FirstOrDefault(stat => stat.Field == $"augmentSkillLevel{slot}");
            if (level == null)
                continue;

            var recordId = skillName.TextValue ?? string.Empty;
            var resolvedName = _skillNames.GetValueOrDefault(recordId);
            result.Add(new SkillBonus
            {
                RecordId = recordId,
                Name = string.IsNullOrWhiteSpace(resolvedName) ||
                       string.Equals(resolvedName, recordId, StringComparison.OrdinalIgnoreCase)
                    ? null
                    : resolvedName,
                Level = level.Value
            });
        }
        return result.Count == 0 ? null : result;
    }

    private static Dictionary<string, double> _extract(StatComputationResult result)
    {
        var values = new Dictionary<string, double>(result.Stats, StringComparer.Ordinal);
        if (result.ProcLines == null)
            return values;

        foreach (var proc in result.ProcLines)
        {
            if (!proc.Min.HasValue)
                continue;

            var minimumField = proc.Field.EndsWith("Modifier", StringComparison.Ordinal)
                ? proc.Field
                : $"{proc.Field}Min";
            values[minimumField] = proc.Min.Value;
            if (proc.Max.HasValue)
                values[$"{proc.Field}Max"] = proc.Max.Value;
            if (proc.DurationMin.HasValue)
                values[$"{proc.Field}DurationMin"] = proc.DurationMin.Value;
        }

        return values;
    }

    private static string _normalize(string text)
    {
        return _numberPattern().Replace(text, "#").Trim();
    }

    [GeneratedRegex(@"(?<![A-Za-z])[-+]?\d+(?:\.\d+)?", RegexOptions.CultureInvariant)]
    private static partial Regex _numberPattern();
}
