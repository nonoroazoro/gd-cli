namespace GdCli.Contracts;

internal sealed class LootCondition
{
    public required string Field { get; init; }

    public required double Value { get; init; }

    public required string? TextValue { get; init; }
}
