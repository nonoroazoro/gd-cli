namespace GdCli.Contracts;

internal sealed class AscendedSkillModifier
{
    public required string RecordId { get; init; }

    public required string Name { get; init; }

    public IReadOnlyList<RawStat> Stats { get; set; } = [];
}
