namespace GdCli.Features.Drops;

internal sealed class DropPathStep
{
    public required string RecordId { get; init; }

    public required string Name { get; init; }

    public required string RecordClass { get; init; }

    public required string Field { get; init; }

    public IReadOnlyList<DropCondition> Conditions { get; init; } = [];
}
