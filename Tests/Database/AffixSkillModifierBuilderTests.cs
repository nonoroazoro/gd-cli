using GdCli.Database;

namespace GdCli.Tests.Database;

public sealed class AffixSkillModifierBuilderTests
{
    [Fact]
    public void BuildPreservesRepeatedModifierAtDifferentOrdinals()
    {
        using var fixture = new TestDatabase();
        fixture.Execute("""
            INSERT INTO records(id, record_id, class, display_name) VALUES
                (20, 'records/items/lootaffixes/prefixunique/variant.dbr', 'LootRandomizer', 'Variant'),
                (21, 'records/skills/itemskills/skillmodifiers/shared.dbr', 'SkillModifier', 'Shared'),
                (22, 'records/skills/player/first.dbr', 'Skill', 'First'),
                (23, 'records/skills/player/second.dbr', 'Skill', 'Second');
            INSERT INTO affixes(record_pk, family, kind, rarity, item_level, required_level, jitter_percent)
            VALUES (20, 'variant', 'prefix', 'Legendary', 94, 94, 0);
            INSERT INTO field_names(id, name) VALUES
                (20, 'modifierSkillName1'),
                (21, 'modifierSkillName2'),
                (22, 'modifiedSkillName1'),
                (23, 'modifiedSkillName2');
            INSERT INTO record_references(source_pk, field_pk, ordinal, target_pk) VALUES
                (20, 20, 0, 21),
                (20, 21, 0, 21),
                (20, 22, 0, 22),
                (20, 23, 0, 23);
            """);

        fixture.Execute(AffixSkillModifierBuilder.Build);

        using var database = new CliDatabase(fixture.Path);
        var modifiers = database.AffixSkillModifiers.Load(
            ["records/items/lootaffixes/prefixunique/variant.dbr"]);

        Assert.Equal(2, modifiers["records/items/lootaffixes/prefixunique/variant.dbr"].Count);
        Assert.Equal(
            ["records/skills/player/first.dbr", "records/skills/player/second.dbr"],
            modifiers["records/items/lootaffixes/prefixunique/variant.dbr"]
                .Select(modifier => modifier.SkillRecordId));
    }
}
