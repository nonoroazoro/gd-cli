namespace GdCli.Contracts;

internal sealed class AcquisitionEntity
{
    public required IReadOnlyList<string> RecordIds { get; init; }

    public required string Name { get; init; }

    public required IReadOnlyList<AcquisitionLocation> Locations { get; init; }
}
