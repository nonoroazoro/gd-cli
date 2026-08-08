namespace GdCli.Database;

internal sealed class InitializationResult
{
    public required string Database { get; init; }

    public required string GameDirectory { get; init; }

    public required string GameLanguage { get; init; }

    public required int Sources { get; init; }

    public required long Records { get; init; }

    public required long ItemFields { get; init; }

    public required long DropEdges { get; init; }

    public required long DropConditions { get; init; }

    public required long Items { get; init; }

    public required long Affixes { get; init; }

    public required long Levels { get; init; }

    public required long Placements { get; init; }

    public required long MonsterDrops { get; init; }

    public required long FileSize { get; init; }
}
