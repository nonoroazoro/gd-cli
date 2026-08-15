namespace GdCli.Contracts;

internal sealed class AffixSkillModifier
{
    public required int Ordinal { get; init; }

    public required string RecordId { get; init; }

    public required string Name { get; init; }

    public required string? SkillRecordId { get; init; }

    public required string? SkillName { get; init; }

    public IReadOnlyList<RawStat> Stats { get; set; } = [];
}
