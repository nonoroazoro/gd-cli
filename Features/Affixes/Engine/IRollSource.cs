namespace GdCli.Features.Affixes.Engine;

/// <summary>
/// Supplies random values to the item stat roll engine.
/// </summary>
internal interface IRollSource
{
    int NextRange(int maximumInclusive);
    double NextUnit();
    void Consume();
}
