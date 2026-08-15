namespace GdCli.Database;

internal sealed class InitializationResult
{
    public required string Database { get; init; }

    public required string GameDirectory { get; init; }

    public required string GameLanguage { get; init; }

    public required int Sources { get; init; }

    public required long Records { get; init; }

    public required long RecordFields { get; init; }

    public required long LootGraphEdges { get; init; }

    public required long LootConditions { get; init; }

    public required long Items { get; init; }

    public required long Affixes { get; init; }

    public required long AscendedAffixes { get; init; }

    public required long Variants { get; init; }

    public required long AffixSkillModifiers { get; init; }

    public required long AffixCompatibilityRelations { get; init; }

    public required long Levels { get; init; }

    public required long Placements { get; init; }

    public required long AcquisitionSources { get; init; }

    public required long Recipes { get; init; }

    public required long Quests { get; init; }

    public required long QuestNodes { get; init; }

    public required long QuestEntities { get; init; }

    public required long FileSize { get; init; }
}
