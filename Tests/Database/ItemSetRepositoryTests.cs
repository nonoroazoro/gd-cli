using GdCli.Database;

namespace GdCli.Tests.Database;

public sealed class ItemSetRepositoryTests
{
    [Fact]
    public void LoadBonusesKeepsTierStatsSeparateFromDefinitions()
    {
        using var fixture = new TestDatabase();
        fixture.Execute("""
            INSERT INTO records(id, record_id, class, template, display_name)
            VALUES (20, 'records/items/lootsets/set.dbr', '', 'database/templates/itemset.tpl', 'Set');
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
                (20, 'offensivePhysicalModifier'),
                (21, 'characterDefensiveAbility'),
                (22, 'augmentSkillLevel1'),
                (23, 'augmentSkillName1');
            INSERT INTO record_fields(record_pk, field_pk, ordinal, numeric_value) VALUES
                (20, 20, 1, 120),
                (20, 20, 2, 180),
                (20, 21, 2, 80),
                (20, 22, 2, 3);
            INSERT INTO record_fields(record_pk, field_pk, ordinal, numeric_value, text_value)
            VALUES (20, 23, 0, 0, 'records/skills/player/skill.dbr');
            """);

        using var database = new CliDatabase(fixture.Path);
        var bonuses = database.ItemSets.LoadBonuses(["records/items/lootsets/set.dbr"]);
        var definitions = database.ItemSets.LoadBonusDefinitions(["records/items/lootsets/set.dbr"]);

        Assert.Equal([2, 3], bonuses["records/items/lootsets/set.dbr"].Select(bonus => bonus.RequiredPieces));
        Assert.Single(bonuses["records/items/lootsets/set.dbr"][0].Stats);
        Assert.DoesNotContain(
            bonuses["records/items/lootsets/set.dbr"][1].Stats,
            stat => stat.Field == "augmentSkillName1");
        Assert.Equal(
            "records/skills/player/skill.dbr",
            Assert.Single(definitions["records/items/lootsets/set.dbr"]).TextValue);
    }
}
