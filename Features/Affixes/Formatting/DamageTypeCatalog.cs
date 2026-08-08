namespace GdCli.Features.Affixes.Formatting;

internal static class DamageTypeCatalog
{
    public static IReadOnlyList<string> BodyFields { get; } =
    [
        "SlowPoison",
        "SlowPhysical",
        "SlowBleeding",
        "SlowLife",
        "SlowFire",
        "SlowCold",
        "SlowLightning",
        "Poison",
        "Chaos",
        "Fire",
        "Aether",
        "Bleeding",
        "Cold",
        "Lightning",
        "Elemental",
        "Pierce",
        "BonusPhysical",
        "Physical",
        "Life",
        "TotalDamage",
        "PercentCurrentLife"
    ];
}
