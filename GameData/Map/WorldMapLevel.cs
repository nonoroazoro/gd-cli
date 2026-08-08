namespace GdCli.GameData.Map;

internal sealed class WorldMapLevel
{
    public required string Path { get; init; }

    public required string RiftGateRecordId { get; init; }

    public required int OffsetX { get; init; }

    public required int OffsetY { get; init; }

    public required int OffsetZ { get; init; }

    public required long DataOffset { get; init; }

    public required int DataLength { get; init; }
}
