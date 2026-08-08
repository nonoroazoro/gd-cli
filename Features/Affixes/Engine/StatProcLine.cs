namespace GdCli.Features.Affixes.Engine;

internal readonly record struct StatProcLine(
    string Field,
    double? Min,
    double? Max,
    double? DurationMin,
    double Chance);
