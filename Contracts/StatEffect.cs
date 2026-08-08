namespace GdCli.Contracts;

internal sealed class StatEffect
{
    public required string Section { get; init; }

    public required string Minimum { get; init; }

    public required string Maximum { get; init; }
}
