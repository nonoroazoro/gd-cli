namespace GdCli.Features.Affixes.Engine;

internal sealed record StatRangeResult(
    StatComputationResult Minimum,
    StatComputationResult Maximum);
