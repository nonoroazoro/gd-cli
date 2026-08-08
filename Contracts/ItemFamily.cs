namespace GdCli.Contracts;

internal sealed class ItemFamily
{
    public required string? NameTag { get; init; }

    public required string Name { get; init; }

    public required bool HasMiRecord { get; init; }

    public required bool HasNonMiRecord { get; init; }

    public required IReadOnlyList<string> RecordIds { get; init; }

    public required IReadOnlyList<string> Rarities { get; init; }
}
