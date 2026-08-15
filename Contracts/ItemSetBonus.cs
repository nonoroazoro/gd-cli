using System.Text.Json.Serialization;

namespace GdCli.Contracts;

internal sealed class ItemSetBonus
{
    public required int RequiredPieces { get; init; }

    public List<RawStat> Stats { get; } = [];

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<SkillModifier>? SkillModifiers { get; set; }
}
