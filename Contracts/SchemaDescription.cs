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

    public required IReadOnlyList<string> AffixFamilies { get; init; }

    public required IReadOnlyList<string> AffixKinds { get; init; }

    public required IReadOnlyList<string> AscendedCategories { get; init; }

    public required IReadOnlyList<string> Availabilities { get; init; }

    public IReadOnlyDictionary<string, string> AvailabilityValues { get; init; } =
        new Dictionary<string, string>
        {
            ["known"] = "supported acquisition or recipe evidence exists",
            ["referenced"] = "live game, map, or quest data references the record",
            ["unresolved"] = "available evidence is insufficient",
            ["unavailable"] = "explicit exclusion evidence exists without a live path"
        };

    public IReadOnlyDictionary<string, string> InfoFields { get; init; } =
        new Dictionary<string, string>
        {
            ["database"] = "absolute CLI database path",
            ["fileSize"] = "database size in bytes",
            ["lastWriteTimeUtc"] = "database modification time in UTC",
            ["sqliteVersion"] = "SQLite runtime version",
            ["userVersion"] = "CLI database schema version",
            ["recordCount"] = "number of imported game records",
            ["itemCount"] = "number of item records",
            ["itemSetCount"] = "number of item set records",
            ["affixCount"] = "number of Prefix and Suffix records",
            ["ascendedAffixCount"] = "number of Ascended affix records",
            ["ascendedSkillModifierCount"] = "number of distinct Ascended skill modifiers",
            ["variantCount"] = "number of game-defined item variants",
            ["variantSkillModifierCount"] = "number of distinct variant skill modifiers",
            ["levelCount"] = "number of map levels",
            ["placementCount"] = "number of relevant map placements",
            ["questCount"] = "number of quest definitions",
            ["questNodeCount"] = "number of quest graph nodes",
            ["questEntityCount"] = "number of quest entity relations",
            ["miRecordCount"] = "number of MI item records",
            ["miNameTagCount"] = "number of distinct name tags containing MI records",
            ["acquisitionSourceCount"] = "number of derived item acquisition sources",
            ["recipeCount"] = "number of crafted item and recipe relations",
            ["gameLanguage"] = "language of parsed game text",
            ["gameDirectory"] = "game directory used by init",
            ["rarities"] = "valid rarity filter values",
            ["itemClasses"] = "valid itemClass filter values",
            ["affixFamilies"] = "standard and ascended affix families",
            ["affixKinds"] = "valid normal affix kind values",
            ["ascendedCategories"] = "valid Ascended category filter values",
            ["availabilities"] = "known, referenced, unresolved, and unavailable item values"
        };

    public IReadOnlyDictionary<string, string> ItemFields { get; init; } =
        new Dictionary<string, string>
        {
            ["recordId"] = "string",
            ["name"] = "string",
            ["nameTag"] = "stable game text tag or null",
            ["rarity"] = "string from itemClassification",
            ["itemClass"] = "string from Class",
            ["itemLevel"] = "number",
            ["requiredLevel"] = "number",
            ["isMi"] = "boolean derived from monster-specific drop relations",
            ["availability"] = "known, referenced, unresolved, or unavailable",
            ["miSources"] = "MonsterSource[]",
            ["stats"] = "RawStat[]",
            ["variants"] = "game-defined variants for a queried item",
            ["acquisition"] = "vendor, monster, random drop, craft, or unknown methods for a queried item"
        };

    public IReadOnlyDictionary<string, string> ItemQueryFields { get; init; } =
        new Dictionary<string, string>
        {
            ["data"] = "matching item records",
            ["itemSets"] = "sets related to queried items; empty when none and omitted for catalog listings"
        };

    public IReadOnlyDictionary<string, string> ItemFamilyFields { get; init; } =
        new Dictionary<string, string>
        {
            ["nameTag"] = "shared game text tag or null for a single-record family",
            ["name"] = "localized family name",
            ["hasMiRecord"] = "boolean",
            ["hasNonMiRecord"] = "boolean",
            ["recordIds"] = "string[]",
            ["rarities"] = "string[]",
            ["availabilities"] = "string[]"
        };

    public IReadOnlyDictionary<string, string> ItemSetFields { get; init; } =
        new Dictionary<string, string>
        {
            ["recordId"] = "stable item set record ID",
            ["name"] = "localized setName text",
            ["nameTag"] = "stable setName tag",
            ["itemLevel"] = "number",
            ["availability"] = "aggregate member availability",
            ["members"] = "ordered item set members",
            ["stats"] = "RawStat[] for set bonuses"
        };

    public IReadOnlyDictionary<string, string> ItemVariantFields { get; init; } =
        new Dictionary<string, string>
        {
            ["recordId"] = "stable unique affix record ID",
            ["name"] = "localized game text or source description",
            ["kind"] = "prefix or suffix",
            ["rarity"] = "string from itemClassification",
            ["itemLevel"] = "number",
            ["requiredLevel"] = "number",
            ["jitterPercent"] = "number from lootRandomizerJitter",
            ["stats"] = "variant RawStat[] with numeric boundaries",
            ["effects"] = "English minimum and maximum direct effect text",
            ["skillBonuses"] = "resolved direct skill level bonuses",
            ["skillModifiers"] = "modified skills and complete skill modifier RawStat[]",
            ["unmodeledFields"] = "raw fields not modeled by the range engine",
            ["sourceRecordIds"] = "dynamic loot tables defining the variant"
        };

    public IReadOnlyDictionary<string, string> AcquisitionFields { get; init; } =
        new Dictionary<string, string>
        {
            ["kind"] = "vendor, specificMonster, randomDrop, craft, or unknown; unknown does not imply unavailable",
            ["recipe"] = "craft recipe or design identity when kind is craft",
            ["sources"] = "known recipe acquisition methods when kind is craft",
            ["actors"] = "grouped source monsters or vendors with all stable record IDs and available map locations",
            ["routes"] = "specific monster loot graph paths with map coordinates",
            ["path"] = "ordered loot graph steps from the item to a placed source",
            ["conditions"] = "game-native weight, chance, equipment, and level conditions for a path step",
            ["routesTruncated"] = "true when route or depth limits prevented a complete result",
            ["routeLimit"] = "maximum number of distinct routes returned",
            ["maximumDepth"] = "maximum runtime loot graph depth"
        };

    public IReadOnlyDictionary<string, string> AffixFields { get; init; } =
        new Dictionary<string, string>
        {
            ["recordId"] = "string",
            ["name"] = "string from lootRandomizerName and ItemTag",
            ["family"] = "standard or ascended",
            ["kind"] = "prefix or suffix for standard affixes",
            ["categories"] = "game-native categories for Ascended affixes",
            ["groups"] = "table groups for Ascended affixes",
            ["rarity"] = "string from itemClassification",
            ["itemLevel"] = "number",
            ["requiredLevel"] = "number",
            ["jitterPercent"] = "number from lootRandomizerJitter",
            ["stats"] = "RawStat[] with numeric boundaries",
            ["effects"] = "English minimum and maximum effect text, including skill bonuses and chance effects",
            ["skillBonuses"] = "resolved skill level bonuses with stable record IDs",
            ["skillModifiers"] = "referenced skill modifier records for Ascended affixes",
            ["unmodeledFields"] = "raw fields not modeled by the range engine"
        };

    public IReadOnlyDictionary<string, string> QuestFields { get; init; } =
        new Dictionary<string, string>
        {
            ["questPath"] = "stable game quest path",
            ["name"] = "localized quest name",
            ["nodes"] = "ordered task, objective, event, conversation, and script nodes with hierarchy, links, and task flags",
            ["edges"] = "quest and task state transitions",
            ["entities"] = "quest actors and targets with available key coordinates",
            ["unresolvedReferences"] = "references that could not be mapped to a game record or actor"
        };

    public IReadOnlyDictionary<string, bool> Capabilities { get; init; } =
        new Dictionary<string, bool>
        {
            ["readOnlyQueries"] = true,
            ["independentDatabase"] = true,
            ["initialization"] = true,
            ["streamingGameData"] = true,
            ["itemAcquisition"] = true,
            ["monsterSpecificAcquisition"] = true,
            ["vendorAcquisition"] = true,
            ["randomDropAcquisition"] = true,
            ["craftAcquisition"] = true,
            ["acquisitionMapLocations"] = true,
            ["itemFamilies"] = true,
            ["itemSets"] = true,
            ["itemVariants"] = true,
            ["itemAvailability"] = true,
            ["jmesPathQuery"] = true,
            ["itemAffixCompatibility"] = true,
            ["ascendedAffixes"] = true,
            ["questGraph"] = true,
            ["questKeyLocations"] = true,
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
