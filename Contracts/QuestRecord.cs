using System.Text.Json.Serialization;

namespace GdCli.Contracts;

internal sealed class QuestRecord
{
    public required string QuestPath { get; init; }

    public required string Source { get; init; }

    public required long Uid { get; init; }

    public required long Flags { get; init; }

    public required string Region { get; init; }

    public required string Name { get; init; }

    public required long TaskCount { get; init; }

    public required long NodeCount { get; init; }

    public required long EntityCount { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<QuestNodeRecord>? Nodes { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<QuestEdgeRecord>? Edges { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<QuestEntityRecord>? Entities { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<QuestUnresolvedReference>? UnresolvedReferences { get; set; }
}
