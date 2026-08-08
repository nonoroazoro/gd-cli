namespace GdCli.Database;

internal sealed record AffixFilter(
    string? Rarity,
    string? Kind,
    string? ItemClass,
    int? MinimumLevel,
    int? MaximumLevel);
