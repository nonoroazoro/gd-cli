using System.Text.Json.Serialization;

namespace GdCli.Contracts;

internal sealed class SkillBonus
{
    public required string RecordId { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Name { get; init; }

    public required double Level { get; init; }
}
