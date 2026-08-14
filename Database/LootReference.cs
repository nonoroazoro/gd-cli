namespace GdCli.Database;

internal sealed class LootReference
{
    public required string SourceRecordId { get; init; }

    public required string SourceName { get; init; }

    public required string SourceClass { get; init; }

    public required string Field { get; init; }
}
