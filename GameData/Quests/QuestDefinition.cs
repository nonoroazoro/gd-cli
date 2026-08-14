namespace GdCli.GameData.Quests;

internal sealed class QuestDefinition
{
    public required string Path { get; init; }

    public required string Source { get; init; }

    public required uint Uid { get; init; }

    public required uint Flags { get; init; }

    public required IReadOnlyList<QuestTask> Tasks { get; init; }

    public string Region { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;
}
