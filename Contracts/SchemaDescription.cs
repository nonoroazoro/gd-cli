using GdCli.Commands;

namespace GdCli.Contracts;

internal sealed class SchemaDescription
{
    public string SchemaVersion { get; init; } = OutputSchema.Version;

    public required string Database { get; init; }

    public IReadOnlyList<string> Commands { get; init; } = CommandCatalog.CommandNames
        .Order(StringComparer.OrdinalIgnoreCase)
        .ToList();

    public required IReadOnlyList<string> Rarities { get; init; }

    public required IReadOnlyList<string> ItemClasses { get; init; }

    public required IReadOnlyList<string> AffixKinds { get; init; }

    public IReadOnlyDictionary<string, string> ItemFields { get; init; } =
        new Dictionary<string, string>
        {
            ["recordId"] = "string",
            ["name"] = "string",
            ["rarity"] = "string from itemClassification",
            ["itemClass"] = "string from Class",
            ["itemLevel"] = "number",
            ["requiredLevel"] = "number",
            ["isMi"] = "boolean derived from monster-specific drop relations",
            ["miSources"] = "MonsterSource[]",
            ["stats"] = "RawStat[]"
        };

    public IReadOnlyDictionary<string, string> DropFields { get; init; } =
        new Dictionary<string, string>
        {
            ["recordId"] = "string",
            ["name"] = "string",
            ["rarity"] = "string from itemClassification",
            ["isMi"] = "boolean",
            ["miSources"] = "MonsterSource[]",
            ["routes"] = "drop graph paths with map coordinates"
        };

    public IReadOnlyDictionary<string, string> AffixFields { get; init; } =
        new Dictionary<string, string>
        {
            ["recordId"] = "string",
            ["name"] = "string from lootRandomizerName and ItemTag",
            ["kind"] = "prefix or suffix",
            ["rarity"] = "string from itemClassification",
            ["itemLevel"] = "number",
            ["requiredLevel"] = "number",
            ["jitterPercent"] = "number from lootRandomizerJitter",
            ["stats"] = "RawStat[] with numeric boundaries",
            ["effects"] = "English minimum and maximum effect text",
            ["unmodeledFields"] = "raw fields not modeled by the range engine"
        };

    public IReadOnlyDictionary<string, bool> Capabilities { get; init; } =
        new Dictionary<string, bool>
        {
            ["readOnlyQueries"] = true,
            ["independentDatabase"] = true,
            ["initialization"] = true,
            ["streamingGameData"] = true,
            ["miDrops"] = true,
            ["miMapLocations"] = true,
            ["jmesPathQuery"] = true,
            ["itemAffixCompatibility"] = false,
            ["ascendedAffixes"] = false,
            ["sourceFile"] = false
        };

    public IReadOnlyDictionary<string, string> ErrorFields { get; init; } =
        new Dictionary<string, string>
        {
            ["code"] = "stable machine-readable string",
            ["error"] = "diagnostic string",
            ["exitCode"] = "number"
        };

    public IReadOnlyDictionary<string, string> PaginationFields { get; init; } =
        new Dictionary<string, string>
        {
            ["count"] = "number of records in this page",
            ["total"] = "number of records matching filters",
            ["offset"] = "zero-based page offset",
            ["limit"] = "requested page size or null for all",
            ["hasMore"] = "boolean",
            ["nextOffset"] = "next page offset when hasMore is true"
        };
}
