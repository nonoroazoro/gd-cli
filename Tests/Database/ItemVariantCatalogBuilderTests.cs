using GdCli.Database;

namespace GdCli.Tests.Database;

public sealed class ItemVariantCatalogBuilderTests
{
    [Fact]
    public void BuildDerivesNestedUniqueAffixAndSkillModifierRelations()
    {
        using var fixture = new TestDatabase();
        fixture.Execute("""
            INSERT INTO records(id, record_id, class, display_name) VALUES
                (20, 'records/items/loottables/dynamic.dbr', 'LootItemTable_DynWeight', 'Dynamic'),
                (21, 'records/items/lootaffixes/prefix/table1.dbr', 'LootRandomizerTable', 'Table 1'),
                (22, 'records/items/lootaffixes/prefixunique/variant.dbr', 'LootRandomizer', 'Variant'),
                (23, 'records/skills/player/skill.dbr', 'Skill', 'Skill'),
                (24, 'records/skills/itemskills/skillmodifiers/variant.dbr', 'SkillModifier', 'Modifier'),
                (25, 'records/items/lootaffixes/prefix/table2.dbr', 'LootRandomizerTable', 'Table 2'),
                (26, 'records/items/lootaffixes/prefixunique/other.dbr', 'LootRandomizer', 'Other'),
                (27, 'records/items/lootaffixes/prefix/other-table.dbr', 'LootRandomizerTable', 'Other Table');
            INSERT INTO field_names(id, name) VALUES
                (20, 'lootName1'),
                (21, 'prefixTableName1'),
                (22, 'randomizerName1'),
                (23, 'modifiedSkillName1'),
                (24, 'modifierSkillName1'),
                (25, 'lootName2'),
                (26, 'prefixTableName2');
            INSERT INTO record_references(source_pk, field_pk, ordinal, target_pk) VALUES
                (20, 20, 0, 2),
                (20, 21, 0, 21),
                (20, 25, 0, 1),
                (20, 26, 0, 27),
                (21, 22, 0, 25),
                (25, 22, 0, 22),
                (27, 22, 0, 26),
                (22, 23, 0, 23),
                (22, 24, 0, 24);
            """);

        fixture.Execute((connection, transaction) =>
        {
            ItemVariantCatalogBuilder.Build(connection, transaction);
            RecordSkillModifierBuilder.Build(connection, transaction);
        });

        using var database = new CliDatabase(fixture.Path);
        var variants = database.ItemVariants.LoadForItems(["records/items/b.dbr"]);
        var variant = Assert.Single(variants["records/items/b.dbr"]);
        var modifier = Assert.Single(database.RecordSkillModifiers.Load([variant.RecordId])[variant.RecordId]);

        Assert.Equal("records/items/lootaffixes/prefixunique/variant.dbr", variant.RecordId);
        Assert.Equal("prefix", variant.Kind);
        Assert.Equal(["records/items/loottables/dynamic.dbr"], variant.SourceRecordIds);
        Assert.Equal("records/skills/player/skill.dbr", modifier.SkillRecordId);
        Assert.Equal("records/skills/itemskills/skillmodifiers/variant.dbr", modifier.RecordId);
    }
}
