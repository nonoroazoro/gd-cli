using System.Text.Json.Serialization;

namespace GdCli.Contracts;

internal sealed class AffixRecord
{
    public required string RecordId { get; init; }

    public required string Name { get; init; }

    public required string Kind { get; init; }

    public required string Rarity { get; init; }

    public required double ItemLevel { get; init; }

    public required double RequiredLevel { get; init; }

    public required double JitterPercent { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<RawStat>? Stats { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<StatEffect>? Effects { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<SkillBonus>? SkillBonuses { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<string>? UnmodeledFields { get; set; }
}
