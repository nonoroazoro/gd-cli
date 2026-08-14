namespace GdCli.Contracts;

internal sealed class QuestEntityRecord
{
    public List<long> NodeIds { get; init; } = [];

    public required string Role { get; init; }

    public required string RecordId { get; init; }

    public required string Name { get; init; }

    public List<string> Origins { get; init; } = [];

    public IReadOnlyList<QuestLocation> Locations { get; set; } = [];
}
