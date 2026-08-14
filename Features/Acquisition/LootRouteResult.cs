using GdCli.Contracts;

namespace GdCli.Features.Acquisition;

internal sealed class LootRouteResult
{
    public required IReadOnlyList<AcquisitionRoute> Routes { get; init; }

    public required bool RoutesTruncated { get; init; }

    public required int RouteLimit { get; init; }

    public required int MaximumDepth { get; init; }
}
