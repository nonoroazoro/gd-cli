using System.Text.Json.Serialization;

namespace GdCli.Contracts;

internal sealed class SearchHit
{
    public required string Entity { get; init; }

    public required string RecordId { get; init; }

    public required string Name { get; init; }

    public required string Rarity { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ItemClass { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Kind { get; init; }

    public required double ItemLevel { get; init; }

    public required double RequiredLevel { get; init; }
}
