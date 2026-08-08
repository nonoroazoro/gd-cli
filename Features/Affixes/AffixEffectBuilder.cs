using System.Text.RegularExpressions;
using GdCli.Contracts;
using GdCli.Features.Affixes.Engine;
using GdCli.Features.Affixes.Formatting;

namespace GdCli.Features.Affixes;

internal sealed class AffixEffectBuilder
{
    private static readonly Regex _numberPattern = new(
        @"(?<![A-Za-z])[-+]?\d+(?:\.\d+)?",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private readonly StatFormatter _formatter;

    public AffixEffectBuilder(IStatTagProvider statTags)
    {
        _formatter = new StatFormatter(statTags);
    }

    public void Apply(AffixRecord affix)
    {
        var stats = affix.Stats ?? throw new InvalidOperationException("Affix stats must be loaded before effect calculation.");
        var input = stats
            .Where(stat => !string.IsNullOrWhiteSpace(stat.Field))
            .GroupBy(stat => stat.Field, StringComparer.Ordinal)
            .Select(group => group.OrderByDescending(stat => stat.Value).First())
            .Select(stat => new StatInput(stat.Field, stat.TextValue ?? string.Empty, stat.Value))
            .ToList();

        var range = affix.Kind.Equals("prefix", StringComparison.OrdinalIgnoreCase)
            ? ItemStatEngine.ComputeRange([], prefixStats: input)
            : ItemStatEngine.ComputeRange([], suffixStats: input);

        var minimum = _extract(range.Minimum);
        var maximum = _extract(range.Maximum);
        foreach (var stat in stats.Where(stat => stat.TextValue == null))
        {
            stat.Minimum = minimum.TryGetValue(stat.Field, out var minimumValue) ? minimumValue : stat.Value;
            stat.Maximum = maximum.TryGetValue(stat.Field, out var maximumValue) ? maximumValue : stat.Value;
        }

        affix.JitterPercent = stats
            .FirstOrDefault(stat => stat.Field == "lootRandomizerJitter")?.Value ?? affix.JitterPercent;
        affix.UnmodeledFields = range.Minimum.UnmodeledFields
            .Concat(range.Maximum.UnmodeledFields)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToList();
        affix.Effects = _buildEffects(stats, minimum, maximum);
    }

    private List<StatEffect> _buildEffects(
        IReadOnlyList<RawStat> raw,
        IReadOnlyDictionary<string, double> minimum,
        IReadOnlyDictionary<string, double> maximum)
    {
        var effects = new List<StatEffect>();
        _addSection(effects, "header", raw, minimum, maximum, StatSection.Header);
        _addSection(effects, "body", raw, minimum, maximum, StatSection.Body);
        _addSection(effects, "pet", raw, minimum, maximum, StatSection.Pet);
        return effects;
    }

    private void _addSection(
        List<StatEffect> output,
        string section,
        IReadOnlyList<RawStat> raw,
        IReadOnlyDictionary<string, double> minimum,
        IReadOnlyDictionary<string, double> maximum,
        StatSection type)
    {
        var minimumText = _formatter.ProcessStats(_buildStats(raw, minimum), type)
            .Select(stat => stat.ToString())
            .Where(text => !string.IsNullOrWhiteSpace(text))
            .Cast<string>()
            .ToList();
        var maximumBuckets = _formatter.ProcessStats(_buildStats(raw, maximum), type)
            .Select(stat => stat.ToString())
            .Where(text => !string.IsNullOrWhiteSpace(text))
            .Cast<string>()
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
        IEnumerable<RawStat> raw,
        IReadOnlyDictionary<string, double> values)
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

    private static Dictionary<string, double> _extract(StatComputationResult result)
    {
        var values = new Dictionary<string, double>(result.Stats, StringComparer.Ordinal);
        if (result.ProcLines == null)
            return values;

        foreach (var proc in result.ProcLines)
        {
            if (!proc.Min.HasValue)
                continue;
            values[proc.Field] = values.TryGetValue(proc.Field, out var current)
                ? current + proc.Min.Value
                : proc.Min.Value;
        }

        return values;
    }

    private static string _normalize(string text)
    {
        return _numberPattern.Replace(text, "#").Trim();
    }
}
