namespace GdCli.Features.Drops;

internal sealed class DropReference
{
    public required string SourceRecordId { get; init; }

    public required string SourceName { get; init; }

    public required string SourceClass { get; init; }

    public required string Field { get; init; }

    public required string TargetRecordId { get; init; }
}
