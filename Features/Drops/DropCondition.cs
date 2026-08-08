namespace GdCli.Features.Drops;

internal sealed class DropCondition
{
    public required string Field { get; init; }

    public required double Value { get; init; }

    public string? TextValue { get; init; }
}
