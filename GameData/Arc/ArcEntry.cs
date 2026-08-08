namespace GdCli.GameData.Arc;

internal sealed class ArcEntry
{
    public required string Path { get; init; }

    public required long DecompressedLength { get; init; }

    public required int PartIndex { get; init; }

    public required int PartCount { get; init; }
}
