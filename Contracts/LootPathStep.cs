namespace GdCli.Contracts;

internal sealed class LootPathStep
{
    public required string RecordId { get; init; }

    public required string Name { get; init; }

    public required string RecordClass { get; init; }

    public required string Field { get; init; }

    public IReadOnlyList<LootCondition> Conditions { get; init; } = [];
}
