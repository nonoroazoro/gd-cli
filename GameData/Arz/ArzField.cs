namespace GdCli.GameData.Arz;

internal sealed class ArzField
{
    public required string Name { get; init; }

    public required int Ordinal { get; init; }

    public required double NumericValue { get; init; }

    public string? TextValue { get; init; }
}
