namespace GdCli.GameData.Arz;

internal sealed class ArzRecordHeader
{
    public required uint RecordNameIndex { get; init; }

    public required uint Offset { get; init; }

    public required int CompressedSize { get; init; }

    public required int UncompressedSize { get; init; }
}
