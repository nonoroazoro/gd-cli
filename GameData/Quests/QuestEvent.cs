using GdCli.GameData.Scriptables;

namespace GdCli.GameData.Quests;

internal sealed class QuestEvent
{
    public required string Phase { get; init; }

    public required int Ordinal { get; init; }

    public required uint Flags { get; init; }

    public required ScriptableGroup Conditions { get; init; }

    public required IReadOnlyList<ScriptableValue> Actions { get; init; }

    public string Name { get; set; } = string.Empty;
}
