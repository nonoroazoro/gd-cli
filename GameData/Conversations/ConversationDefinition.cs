namespace GdCli.GameData.Conversations;

internal sealed class ConversationDefinition
{
    public required string Path { get; init; }

    public required ConversationStep Root { get; init; }
}
