namespace GdCli.GameData.Arz;

internal sealed class ArzRecord
{
    public required string RecordId { get; init; }

    public required IReadOnlyList<ArzField> Fields { get; init; }
}
