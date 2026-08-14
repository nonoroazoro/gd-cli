namespace GdCli.GameData.Scriptables;

internal sealed class ScriptableValue
{
    public required string Kind { get; init; }

    public int? Comparison { get; init; }

    public string? QuestPath { get; init; }

    public uint? TaskUid { get; init; }

    public uint? ObjectiveUid { get; init; }

    public string? RecordId { get; init; }

    public IReadOnlyList<string> RecordIds { get; init; } = [];

    public string? Token { get; init; }

    public string? Function { get; init; }

    public string? TextValue { get; init; }

    public double? NumericValue { get; init; }

    public double? SecondaryNumericValue { get; init; }

    public double? TertiaryNumericValue { get; init; }

    public bool? BooleanValue { get; init; }
}
