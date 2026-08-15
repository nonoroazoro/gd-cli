using GdCli.Database;

namespace GdCli.Tests.Database;

public sealed class AscendedAffixBuilderTests
{
    [Theory]
    [InlineData("accessoryTablesAffix1", "accessory", "affix")]
    [InlineData("armorTablesMastery1", "armor", "mastery")]
    [InlineData("offhandTablesAffix1", "offhand", "affix")]
    [InlineData("oneHandMeleeTablesAffix1", "oneHandMelee", "affix")]
    [InlineData("oneHandRangedTablesAffix1", "oneHandRanged", "affix")]
    [InlineData("shieldTablesAffix1", "shield", "affix")]
    [InlineData("twoHandMeleeTablesAffix1", "twoHandMelee", "affix")]
    [InlineData("twoHandRangedTablesAffix1", "twoHandRanged", "affix")]
    public void BuildMapsEveryGameNativeCategory(
        string formulaField,
        string expectedCategory,
        string expectedGroup)
    {
        using var fixture = new TestDatabase();
        fixture.Execute((connection, transaction) =>
        {
            using var setup = connection.CreateCommand();
            setup.Transaction = transaction;
            setup.CommandText = """
                DELETE FROM affix_skill_modifiers;
                DELETE FROM ascended_affix_categories;
                DELETE FROM affixes WHERE family = 'ascended';
                INSERT INTO records(id, record_id, class, display_name) VALUES
                    (30, 'records/items/crafting/ascension/formula.dbr', 'ItemAscensionFormula', 'formula'),
                    (31, 'records/items/lootaffixes/ascended/table.dbr', 'LootRandomizerTable', 'table');
                INSERT INTO field_names(id, name) VALUES
                    (30, @formulaField),
                    (31, 'randomizerName1');
                INSERT INTO record_references(source_pk, field_pk, ordinal, target_pk) VALUES
                    (30, 30, 0, 31),
                    (31, 31, 0, 900);
                """;
            setup.Parameters.AddWithValue("@formulaField", formulaField);
            setup.ExecuteNonQuery();
            AscendedAffixBuilder.Build(connection, transaction);
        });

        using var database = new CliDatabase(fixture.Path);
        var affix = Assert.Single(database.Affixes.Load(
            new AffixFilter("ascended", null, null, null, expectedCategory, null, null),
            0,
            null));

        Assert.Equal([expectedCategory], affix.Categories);
        Assert.Equal([expectedGroup], affix.Groups);
    }

    [Fact]
    public void BuildDerivesNativeCategoryAndSkillModifierRelations()
    {
        using var fixture = new TestDatabase();
        fixture.Execute((connection, transaction) =>
        {
            using var setup = connection.CreateCommand();
            setup.Transaction = transaction;
            setup.CommandText = """
                DELETE FROM affix_skill_modifiers;
                DELETE FROM ascended_affix_categories;
                DELETE FROM affixes WHERE family = 'ascended';
                INSERT INTO records(id, record_id, class, display_name) VALUES
                    (30, 'records/items/crafting/ascension/formula.dbr', 'ItemAscensionFormula', 'formula'),
                    (31, 'records/items/lootaffixes/ascended/table.dbr', 'LootRandomizerTable', 'table');
                INSERT INTO field_names(id, name) VALUES
                    (30, 'oneHandMeleeTablesAffix1'),
                    (31, 'randomizerName1'),
                    (32, 'modifierSkillName1');
                INSERT INTO record_references(source_pk, field_pk, ordinal, target_pk) VALUES
                    (30, 30, 0, 31),
                    (31, 31, 0, 900),
                    (900, 32, 0, 901);
                """;
            setup.ExecuteNonQuery();
            AscendedAffixBuilder.Build(connection, transaction);
            AffixSkillModifierBuilder.Build(connection, transaction);
        });

        using var database = new CliDatabase(fixture.Path);
        var affix = Assert.Single(database.Affixes.Load(
            new AffixFilter("ascended", null, null, null, "oneHandMelee", null, null),
            0,
            null));

        Assert.Equal(["oneHandMelee"], affix.Categories);
        Assert.Equal(["affix"], affix.Groups);
        Assert.Single(database.AffixSkillModifiers.Load([affix.RecordId])[affix.RecordId]);
    }
}
