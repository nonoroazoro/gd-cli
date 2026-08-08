using GdCli.Contracts;

namespace GdCli.Features.Drops;

internal sealed class ItemDropResult
{
    public required string RecordId { get; init; }

    public required string Name { get; init; }

    public required string? NameTag { get; init; }

    public required string Rarity { get; init; }

    public required bool IsMi { get; init; }

    public required IReadOnlyList<MonsterSource> MiSources { get; init; }

    public required IReadOnlyList<DropRoute> Routes { get; init; }

    public required bool RoutesTruncated { get; init; }

    public required int RouteLimit { get; init; }

    public required int MaximumDepth { get; init; }
}
