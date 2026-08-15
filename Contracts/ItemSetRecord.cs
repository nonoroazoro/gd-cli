using System.Text.Json.Serialization;

namespace GdCli.Contracts;

internal sealed class ItemSetRecord
{
    public required string RecordId { get; init; }

    public required string Name { get; init; }

    public required string? NameTag { get; init; }

    public required double ItemLevel { get; init; }

    public required string Availability { get; init; }

    public List<ItemSetMember> Members { get; } = [];

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<RawStat>? Stats { get; set; }
}
