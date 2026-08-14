namespace GdCli.Contracts;

internal sealed class DatabaseInfo
{
    public string SchemaVersion { get; init; } = OutputSchema.Version;

    public required string Database { get; init; }

    public required long FileSize { get; init; }

    public required DateTime LastWriteTimeUtc { get; init; }

    public required string SqliteVersion { get; init; }

    public required long UserVersion { get; init; }

    public required long ItemCount { get; init; }

    public required long AffixCount { get; init; }

    public required long AscendedAffixCount { get; init; }

    public required long AscendedSkillModifierCount { get; init; }

    public required long RecordCount { get; init; }

    public required long LevelCount { get; init; }

    public required long PlacementCount { get; init; }

    public required long QuestCount { get; init; }

    public required long QuestNodeCount { get; init; }

    public required long QuestEntityCount { get; init; }

    public required long MiCount { get; init; }

    public required long MiRecordCount { get; init; }

    public required long MiNameTagCount { get; init; }

    public required string GameLanguage { get; init; }

    public required string GameDirectory { get; init; }

    public required IReadOnlyList<string> Rarities { get; init; }

    public required IReadOnlyList<string> ItemClasses { get; init; }

    public required IReadOnlyList<string> AffixKinds { get; init; }

    public required IReadOnlyList<string> AscendedCategories { get; init; }
}
