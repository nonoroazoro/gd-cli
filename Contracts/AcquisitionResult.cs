namespace GdCli.Contracts;

internal sealed class AcquisitionResult
{
    public required AcquisitionItem Item { get; init; }

    public required IReadOnlyList<AcquisitionMethod> Methods { get; init; }
}
