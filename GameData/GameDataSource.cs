namespace GdCli.GameData;

internal sealed class GameDataSource
{
    public required string Name { get; init; }

    public required int Priority { get; init; }

    public required string Root { get; init; }

    public required string ArzPath { get; init; }

    public string? EnglishTagsPath { get; init; }

    public string? LocalizedTagsPath { get; init; }

    public string? LevelsPath { get; init; }

    public string? QuestsPath { get; init; }

    public string? ConversationsPath { get; init; }

    public string? ScriptsPath { get; init; }
}
