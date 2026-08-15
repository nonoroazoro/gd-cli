using GdCli.Application;
using GdCli.Commands;
using GdCli.Contracts;
using GdCli.Database;
using GdCli.Tests.Database;

namespace GdCli.Tests.Application;

public sealed class ItemsCommandTests
{
    [Fact]
    public void QueryExpandsItemAndTieredSetSkillModifiers()
    {
        using var fixture = new TestDatabase();
        fixture.Execute("""
            INSERT INTO records(id, record_id, class, display_name) VALUES
                (20, 'records/items/lootsets/test.dbr', 'ItemSet', 'Test Set'),
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
            VALUES
                (20, 2, 1, 0),
                (20, 3, 2, 1);
            INSERT INTO field_names(id, name) VALUES
                (20, 'modifierSkillName1'),
                (21, 'modifiedSkillName1'),
                (23, 'offensivePhysicalModifier'),
                (24, 'skillCooldownReduction'),
                (25, 'augmentSkillLevel1'),
                (26, 'augmentSkillName1');
            INSERT INTO record_references(source_pk, field_pk, ordinal, target_pk) VALUES
                (2, 20, 0, 21),
                (2, 21, 0, 23),
                (20, 20, 0, 22),
                (20, 21, 0, 23);
            INSERT INTO record_fields(record_pk, field_pk, ordinal, numeric_value) VALUES
                (20, 23, 1, 120),
                (20, 23, 2, 180),
                (20, 25, 2, 3),
                (21, 24, 0, 20),
                (22, 24, 0, 30);
            INSERT INTO record_fields(record_pk, field_pk, ordinal, numeric_value, text_value)
            VALUES (20, 26, 0, 0, 'records/skills/player/skill.dbr');
            """);
        fixture.Execute(RecordSkillModifierBuilder.Build);
        using var database = new CliDatabase(fixture.Path);
        var options = CommandLineParser.Parse(["items", "Beta", "--all"]);
        CommandLineValidator.Validate(options);

        var result = Assert.IsType<ItemQueryEnvelope>(new ItemsCommand(database).Execute(options));

        Assert.Equal(20, Assert.Single(Assert.Single(result.Data).SkillModifiers ?? []).Stats[0].Value);
        var itemSet = Assert.Single(result.ItemSets ?? []);
        Assert.Equal([2, 3], (itemSet.Bonuses ?? []).Select(bonus => bonus.RequiredPieces));
        var finalBonus = (itemSet.Bonuses ?? [])[1];
        Assert.Equal(30, Assert.Single(finalBonus.SkillModifiers ?? []).Stats[0].Value);
        Assert.Contains(finalBonus.Stats, stat => stat.Field == "augmentSkillName1");
    }

    [Fact]
    public void QueryAggregatesRelationsWithoutLoadingStats()
    {
        using var fixture = new TestDatabase();
        fixture.Execute("""
            INSERT INTO records(id, record_id, class, display_name) VALUES
                (20, 'records/items/lootsets/test.dbr', 'ItemSet', 'Test Set'),
                (21, 'records/items/lootaffixes/prefixunique/test.dbr', 'LootRandomizer', 'Test Variant'),
                (22, 'records/items/loottables/test.dbr', 'LootItemTable_DynWeight', 'Test Source'),
                (23, 'records/creatures/test.dbr', 'Monster', 'Test Monster');
            INSERT INTO item_sets(record_pk, item_level, availability)
            VALUES (20, 10, 'known');
            INSERT INTO item_set_members(set_pk, item_pk, ordinal)
            VALUES (20, 2, 0);
            INSERT INTO affixes(record_pk, family, kind, rarity, item_level, required_level, jitter_percent)
            VALUES (21, 'variant', 'prefix', 'Rare', 10, 10, 0);
            INSERT INTO item_variants(item_pk, affix_pk, source_pk)
            VALUES (2, 21, 22);
            INSERT INTO acquisition_sources(item_pk, kind, source_pk) VALUES
                (2, 'specificMonster', 23),
                (2, 'randomDrop', NULL);
            """);
        using var database = new CliDatabase(fixture.Path);
        var options = CommandLineParser.Parse(["items", "Test", "Set", "--all", "--no-stats"]);
        CommandLineValidator.Validate(options);

        var result = Assert.IsType<ItemQueryEnvelope>(new ItemsCommand(database).Execute(options));

        var item = Assert.Single(result.Data);
        Assert.Null(item.Stats);
        Assert.Single(item.Variants ?? []);
        Assert.Single(item.MiSources ?? []);
        Assert.Equal(
            ["specificMonster", "randomDrop"],
            item.Acquisition?.Select(method => method.Kind));
        Assert.Single(result.ItemSets ?? []);
    }
}
