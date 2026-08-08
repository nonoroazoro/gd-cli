namespace GdCli.Features.Affixes.Formatting;

internal sealed class StatValue
{
    public required string Field { get; init; }

    public float Value { get; init; }

    public string? TextValue { get; init; }
}
