using GdCli.Contracts;
using GdCli.Features.Affixes;
using GdCli.Features.Affixes.Formatting;

namespace GdCli.Tests.Features.Affixes;

public sealed class AffixEffectBuilderTests
{
    [Fact]
    public void ApplyCalculatesNumericRangesAndEffects()
    {
        var affix = new AffixRecord
        {
            RecordId = "records/items/lootaffixes/prefix/test.dbr",
            Name = "Test",
            Kind = "prefix",
            Rarity = "Rare",
            ItemLevel = 1,
            RequiredLevel = 1,
            JitterPercent = 10,
            Stats =
            [
                new RawStat { Field = "characterOffensiveAbility", Value = 40 },
                new RawStat { Field = "defensiveFireMaxResist", Value = 3 },
                new RawStat { Field = "lootRandomizerJitter", Value = 10 }
            ]
        };

        new AffixEffectBuilder(new EnglishStatTags(
            new Dictionary<string, string>(StringComparer.Ordinal))).Apply(affix);

        var offensiveAbility = Assert.Single(
            affix.Stats,
            stat => stat.Field == "characterOffensiveAbility");
        Assert.Equal(36, offensiveAbility.Minimum);
        Assert.Equal(44, offensiveAbility.Maximum);
        Assert.NotNull(affix.Effects);
        var effect = Assert.Single(
            affix.Effects,
            effect => effect.Minimum.Contains("Offensive Ability", StringComparison.Ordinal));
        Assert.Equal("+36 Offensive Ability", effect.Minimum);
        Assert.Equal("+44 Offensive Ability", effect.Maximum);
        var maximumResistance = Assert.Single(
            affix.Stats,
            stat => stat.Field == "defensiveFireMaxResist");
        Assert.Equal(3, maximumResistance.Minimum);
        Assert.Equal(3, maximumResistance.Maximum);
        Assert.Empty(affix.UnmodeledFields ?? []);
    }
}
