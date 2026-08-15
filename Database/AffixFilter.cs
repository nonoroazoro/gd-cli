namespace GdCli.Database;

internal sealed record AffixFilter(
    string? Family,
    string? Rarity,
    string? Kind,
    string? ItemClass,
    string? Category,
    int? MinimumLevel,
    int? MaximumLevel,
    string? Query = null,
    bool ExactQuery = false);
