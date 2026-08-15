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
            Family = "standard",
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

    [Fact]
    public void ApplyIncludesChanceDamageAndResolvedSkillBonuses()
    {
        const string skillRecord = "records/skills/playerclass09/wpattack02.dbr";
        var affix = new AffixRecord
        {
            RecordId = "records/items/lootaffixes/suffix/test.dbr",
            Name = "Test",
            Family = "standard",
            Kind = "suffix",
            Rarity = "Rare",
            ItemLevel = 1,
            RequiredLevel = 1,
            JitterPercent = 0,
            Stats =
            [
                new RawStat { Field = "Class", TextValue = "LootRandomizer" },
                new RawStat { Field = "augmentSkillLevel1", Value = 2 },
                new RawStat { Field = "augmentSkillName1", TextValue = skillRecord },
                new RawStat { Field = "lootRandomizerJitter", Value = 0 },
                new RawStat { Field = "offensiveBonusPhysicalChance", Value = 10 },
                new RawStat { Field = "offensiveBonusPhysicalMin", Value = 133 },
                new RawStat { Field = "offensivePhysicalModifier", Value = 40 },
                new RawStat { Field = "offensivePhysicalModifierChance", Value = 20 }
            ]
        };

        new AffixEffectBuilder(
            new EnglishStatTags(new Dictionary<string, string>(StringComparer.Ordinal)),
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [skillRecord] = "Righteous Fervor"
            }).Apply(affix);

        var physicalDamage = Assert.Single(
            affix.Stats,
            stat => stat.Field == "offensiveBonusPhysicalMin");
        Assert.Equal(133, physicalDamage.Minimum);
        Assert.Equal(133, physicalDamage.Maximum);
        Assert.Contains(
            affix.Effects ?? [],
            effect => effect.Minimum == "10% Chance of 133 Physical Damage" &&
                      effect.Maximum == effect.Minimum);
        Assert.Contains(
            affix.Effects ?? [],
            effect => effect.Minimum == "20% Chance of +40% Physical Damage" &&
                      effect.Maximum == effect.Minimum);
        Assert.Contains(
            affix.Effects ?? [],
            effect => effect.Minimum == "+2 to Righteous Fervor" &&
                      effect.Maximum == effect.Minimum);
        var skillBonus = Assert.Single(affix.SkillBonuses ?? []);
        Assert.Equal(skillRecord, skillBonus.RecordId);
        Assert.Equal("Righteous Fervor", skillBonus.Name);
        Assert.Equal(2, skillBonus.Level);
        Assert.Empty(affix.UnmodeledFields ?? []);
    }
}
