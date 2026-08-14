using System.Text.Json.Serialization;

namespace GdCli.Contracts;

internal sealed class QuestEdgeRecord
{
    public required long SourceNodeId { get; init; }

    public required string TargetQuestPath { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public long? TargetTaskUid { get; init; }

    public required string Kind { get; init; }

    public required string Origin { get; init; }
}
