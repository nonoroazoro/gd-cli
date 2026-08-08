namespace GdCli.GameData.Arc;

internal sealed class ArcPart
{
    public required int Offset { get; init; }

    public required int CompressedLength { get; init; }

    public required int DecompressedLength { get; init; }
}
