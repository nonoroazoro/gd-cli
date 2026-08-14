using GdCli.Contracts;

namespace GdCli.Features.Acquisition;

internal sealed record LootSearchState(
    string RecordId,
    IReadOnlyList<LootPathStep> Path,
    IReadOnlySet<string> Visited);
