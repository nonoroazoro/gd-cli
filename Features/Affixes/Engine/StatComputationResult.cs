namespace GdCli.Features.Affixes.Engine;

internal sealed record StatComputationResult(
    IReadOnlyDictionary<string, double> Stats,
    IReadOnlyList<string> UnmodeledFields,
    IReadOnlyList<StatProcLine>? ProcLines = null);
