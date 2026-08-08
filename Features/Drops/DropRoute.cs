namespace GdCli.Features.Drops;

internal sealed class DropRoute
{
    public required IReadOnlyList<DropPathStep> Path { get; init; }

    public required DropLocation Location { get; init; }
}
