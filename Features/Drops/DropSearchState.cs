namespace GdCli.Features.Drops;

internal sealed record DropSearchState(
    string RecordId,
    IReadOnlyList<DropPathStep> Path,
    HashSet<string> Visited);
