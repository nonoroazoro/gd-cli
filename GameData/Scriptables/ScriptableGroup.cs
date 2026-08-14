namespace GdCli.GameData.Scriptables;

internal sealed class ScriptableGroup
{
    public required string Operator { get; init; }

    public required IReadOnlyList<ScriptableValue> Values { get; init; }
}
