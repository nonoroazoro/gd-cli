namespace GdCli.GameData.Map;

internal sealed class PlacedObject
{
    public required string LevelPath { get; init; }

    public required string RiftGateRecordId { get; init; }

    public required int EntityOrdinal { get; init; }

    public required string RecordId { get; init; }

    public required double WorldX { get; init; }

    public required double WorldY { get; init; }

    public required double WorldZ { get; init; }
}
