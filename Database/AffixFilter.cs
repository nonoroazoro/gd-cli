namespace GdCli.Database;

internal sealed record AffixFilter(
    string? Rarity,
    string? Kind,
    int? MinimumLevel,
    int? MaximumLevel);
