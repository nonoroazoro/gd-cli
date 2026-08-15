using GdCli.Database;

namespace GdCli.Tests.Database;

public sealed class ItemSetCatalogBuilderTests
{
    [Fact]
    public void BuildCreatesOnlySetsWithMembersAndPreservesOrder()
    {
        using var fixture = new TestDatabase();
        fixture.Execute("""
            INSERT INTO records(id, record_id, class, template, name_tag, display_name) VALUES
                (20, 'records/items/lootsets/set.dbr', '', 'database/templates/itemset.tpl', 'tagSet', 'Localized Set'),
                (21, 'records/storyelements/signs/signset.dbr', '', 'database/templates/itemset.tpl', 'tagSign', 'Sign Set'),
                (22, 'records/storyelements/signs/sign01.dbr', '', 'database/templates/sign.tpl', 'tagSign01', 'Sign');
            INSERT INTO field_names(id, name) VALUES (20, 'setMembers');
            INSERT INTO record_references(source_pk, field_pk, ordinal, target_pk) VALUES
                (20, 20, 1, 3),
                (20, 20, 0, 2),
                (21, 20, 0, 22);
            """);

        fixture.Execute(ItemSetCatalogBuilder.Build);

        using var database = new CliDatabase(fixture.Path);
        var itemSet = Assert.Single(database.ItemSets.LoadForItems(["records/items/b.dbr"]));

        Assert.Equal("Localized Set", itemSet.Name);
        Assert.Equal("tagSet", itemSet.NameTag);
        Assert.Equal(["records/items/b.dbr", "records/items/c.dbr"], itemSet.Members.Select(member => member.RecordId));
    }
}
