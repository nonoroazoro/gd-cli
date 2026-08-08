namespace GdCli.Database;

internal sealed class InitializationProgress
{
    public required string Stage { get; init; }

    public required string Source { get; init; }

    public required int Current { get; init; }

    public required int Total { get; init; }
}
