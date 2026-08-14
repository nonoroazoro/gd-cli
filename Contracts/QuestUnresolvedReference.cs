using System.Text.Json.Serialization;

namespace GdCli.Contracts;

internal sealed class QuestUnresolvedReference
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public long? NodeId { get; init; }

    public required string Kind { get; init; }

    public required string Value { get; init; }

    public required string Origin { get; init; }
}
