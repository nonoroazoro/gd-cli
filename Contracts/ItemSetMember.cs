namespace GdCli.Contracts;

internal sealed class ItemSetMember
{
    public required string RecordId { get; init; }

    public required string Name { get; init; }

    public required string Rarity { get; init; }

    public required string ItemClass { get; init; }

    public required double RequiredLevel { get; init; }

    public required string Availability { get; init; }
}
