using GdCli.GameData.Scriptables;

namespace GdCli.GameData.Conversations;

internal sealed class ConversationStep
{
    public required int Ordinal { get; init; }

    public required string Type { get; init; }

    public required uint Flags { get; init; }

    public required int? LinkId { get; init; }

    public required ScriptableGroup Conditions { get; init; }

    public required IReadOnlyList<ScriptableValue> Actions { get; init; }

    public required IReadOnlyList<ConversationStep> Children { get; init; }
}
