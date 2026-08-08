using System.Text.Json.Serialization;

namespace GdCli.Contracts;

internal sealed class QueryEnvelope<T>
{
    public string SchemaVersion { get; init; } = OutputSchema.Version;

    public required string Command { get; init; }

    public required string Database { get; init; }

    public required int Count { get; init; }

    public required int Total { get; init; }

    public required int Offset { get; init; }

    public int? Limit { get; init; }

    public required bool HasMore { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? NextOffset { get; init; }

    public required IReadOnlyList<T> Data { get; init; }
}
