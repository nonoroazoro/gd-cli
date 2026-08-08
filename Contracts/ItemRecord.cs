using System.Text.Json.Serialization;

namespace GdCli.Contracts;

internal sealed class ItemRecord
{
    public required string RecordId { get; init; }

    public required string Name { get; init; }

    public required string? NameTag { get; init; }

    public required string Rarity { get; init; }

    public required string ItemClass { get; init; }

    public required double ItemLevel { get; init; }

    public required double RequiredLevel { get; init; }

    public required bool IsMi { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<MonsterSource>? MiSources { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<RawStat>? Stats { get; set; }
}
