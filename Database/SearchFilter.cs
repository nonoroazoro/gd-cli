namespace GdCli.Database;

internal sealed record SearchFilter(
    string Query,
    string? Rarity,
    string? ItemClass,
    string? Kind,
    int? MinimumLevel,
    int? MaximumLevel);
