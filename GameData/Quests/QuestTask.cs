namespace GdCli.GameData.Quests;

internal sealed class QuestTask
{
    public required int Ordinal { get; init; }

    public required uint Uid { get; init; }

    public required uint Flags { get; init; }

    public required bool IsBlocker { get; init; }

    public required bool DontPropagate { get; init; }

    public required IReadOnlyList<QuestEvent> OnAccept { get; init; }

    public required IReadOnlyList<QuestObjective> Objectives { get; init; }

    public required IReadOnlyList<QuestEvent> OnComplete { get; init; }

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;
}
