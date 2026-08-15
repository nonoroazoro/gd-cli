namespace GdCli.Database;

internal sealed record ItemFilter(
    string? Rarity,
    string? ItemClass,
    int? MinimumLevel,
    int? MaximumLevel,
    bool? IsMi,
    string? Availability = null,
    bool IncludeUnavailable = false,
    string? Query = null,
    bool ExactQuery = false);
