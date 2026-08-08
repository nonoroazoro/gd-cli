namespace GdCli.Features.Drops;

internal sealed class DropLocation
{
    public required string Source { get; init; }

    public required string Level { get; init; }

    public required string RiftGateRecordId { get; init; }

    public required double X { get; init; }

    public required double Y { get; init; }

    public required double Z { get; init; }
}
