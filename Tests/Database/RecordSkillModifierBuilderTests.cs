using GdCli.Database;

namespace GdCli.Tests.Database;

public sealed class RecordSkillModifierBuilderTests
{
    [Fact]
    public void BuildIndexesItemAndSetOwners()
    {
        using var fixture = new TestDatabase();
        fixture.Execute("""
            INSERT INTO records(id, record_id, class, display_name) VALUES
                (20, 'records/items/lootsets/set.dbr', 'ItemSet', 'Set'),
                (21, 'records/skills/itemskills/skillmodifiers/item.dbr', 'SkillModifier', 'Item Modifier'),
                (22, 'records/skills/itemskills/skillmodifiers/set.dbr', 'SkillModifier', 'Set Modifier'),
                (23, 'records/skills/player/skill.dbr', 'Skill', 'Skill');
            INSERT INTO item_sets(record_pk, item_level) VALUES (20, 10);
            INSERT INTO item_set_members(set_pk, item_pk, ordinal) VALUES
                (20, 1, 0),
                (20, 2, 1),
                (20, 3, 2);
            INSERT INTO item_set_bonuses(
                set_pk, required_pieces, field_ordinal, has_skill_modifiers)
            VALUES (20, 3, 2, 1);
            INSERT INTO field_names(id, name) VALUES
                (20, 'modifierSkillName1'),
                (21, 'modifiedSkillName1');
            INSERT INTO record_references(source_pk, field_pk, ordinal, target_pk) VALUES
                (2, 20, 0, 21),
                (2, 21, 0, 23),
                (20, 20, 0, 22),
                (20, 21, 0, 23);
            """);

        fixture.Execute(RecordSkillModifierBuilder.Build);

        using var database = new CliDatabase(fixture.Path);
        Assert.Single(database.RecordSkillModifiers.Load(["records/items/b.dbr"])["records/items/b.dbr"]);
        var setModifiers = database.RecordSkillModifiers.LoadSetBonuses(["records/items/lootsets/set.dbr"]);
        Assert.Single(setModifiers["records/items/lootsets/set.dbr"][3]);
    }

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

        fixture.Execute(RecordSkillModifierBuilder.Build);

        using var database = new CliDatabase(fixture.Path);
        var modifiers = database.RecordSkillModifiers.Load(
            ["records/items/lootaffixes/prefixunique/variant.dbr"]);

        Assert.Equal(2, modifiers["records/items/lootaffixes/prefixunique/variant.dbr"].Count);
        Assert.Equal(
            ["records/skills/player/first.dbr", "records/skills/player/second.dbr"],
            modifiers["records/items/lootaffixes/prefixunique/variant.dbr"]
                .Select(modifier => modifier.SkillRecordId));
    }
}
