namespace GdCli.Contracts;

internal sealed class AcquisitionRoute
{
    public required IReadOnlyList<LootPathStep> Path { get; init; }

    public required AcquisitionLocation Location { get; init; }
}
