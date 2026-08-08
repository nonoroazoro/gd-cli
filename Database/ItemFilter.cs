namespace GdCli.Database;

internal sealed record ItemFilter(
    string? Rarity,
    string? ItemClass,
    int? MinimumLevel,
    int? MaximumLevel);
