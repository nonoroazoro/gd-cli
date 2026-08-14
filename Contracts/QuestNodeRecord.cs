using System.Text.Json.Serialization;

namespace GdCli.Contracts;

internal sealed class QuestNodeRecord
{
    public required long NodeId { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public long? ParentNodeId { get; init; }

    public required int Ordinal { get; init; }

    public required string Kind { get; init; }

    public required string Phase { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public long? Uid { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? LinkId { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? IsBlocker { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? DontPropagate { get; init; }

    public required string Name { get; init; }

    public required string Description { get; init; }

    public required long Flags { get; init; }

    public required string ConditionOperator { get; init; }

    public required string Origin { get; init; }

    public IReadOnlyList<QuestOperationRecord> Conditions { get; set; } = [];

    public IReadOnlyList<QuestOperationRecord> Actions { get; set; } = [];
}
