namespace GdCli.Contracts;

internal sealed class ItemSummary
{
    public required string RecordId { get; init; }

    public required string Name { get; init; }

    public required string? NameTag { get; init; }

    public required string Rarity { get; init; }

    public required string ItemClass { get; init; }

    public required string Availability { get; init; }
}
