namespace GdCli.Database;

internal sealed class AcquisitionSourceRecord
{
    public required string Kind { get; init; }

    public required string? RecordId { get; init; }

    public required string? Name { get; init; }

    public required string? NameTag { get; init; }
}
