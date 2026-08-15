using GdCli.Database;

namespace GdCli.Tests.Database;

public sealed class ItemAvailabilityBuilderTests
{
    [Fact]
    public void BuildSeparatesKnownReferencedUnresolvedAndUnavailableItems()
    {
        using var fixture = new TestDatabase();
        fixture.Execute("""
            INSERT INTO records(id, record_id, class, template, name_tag, display_name) VALUES
                (20, 'records/items/lootsets/disabled.dbr', '', 'database/templates/itemset.tpl', 'tagSet', 'Disabled Set'),
                (21, 'records/ui/convert.dbr', '', '', NULL, 'Convert'),
                (22, 'records/runtime/source.dbr', '', '', NULL, 'Runtime Source'),
                (30, 'records/items/unresolved.dbr', 'Item', '', 'tagUnresolved', 'Unresolved');
            INSERT INTO items(record_pk, rarity, item_class, item_level, required_level, is_mi)
            VALUES (30, 'Common', 'Item', 1, 1, 0);
            INSERT INTO field_names(id, name) VALUES
                (20, 'setMembers'),
                (21, 'itemSetName'),
                (22, 'blacklistedSets'),
                (23, 'runtimeItem');
            INSERT INTO record_references(source_pk, field_pk, ordinal, target_pk) VALUES
                (20, 20, 0, 3),
                (20, 20, 1, 4),
                (3, 21, 0, 20),
                (4, 21, 0, 20),
                (21, 22, 0, 20),
                (22, 23, 0, 2);
            INSERT INTO acquisition_sources(item_pk, kind, source_pk)
            VALUES (1, 'randomDrop', NULL);
            """);
        fixture.Execute(ItemSetCatalogBuilder.Build);
        fixture.Execute(ItemAvailabilityBuilder.Build);

        using var database = new CliDatabase(fixture.Path);
        var items = database.Items
            .Load(new ItemFilter(null, null, null, null, null, IncludeUnavailable: true), 0, null)
            .ToDictionary(item => item.RecordId, StringComparer.OrdinalIgnoreCase);

        Assert.Equal("known", items["records/items/a.dbr"].Availability);
        Assert.Equal("referenced", items["records/items/b.dbr"].Availability);
        Assert.Equal("unavailable", items["records/items/c.dbr"].Availability);
        Assert.Equal("unavailable", items["records/items/percent.dbr"].Availability);
        Assert.Equal("unresolved", items["records/items/unresolved.dbr"].Availability);
        var itemSet = Assert.Single(database.ItemSets.LoadForItems(["records/items/c.dbr"]));
        Assert.Equal("unavailable", itemSet.Availability);

        var defaultItems = database.Items.Load(new ItemFilter(null, null, null, null, null), 0, null);
        Assert.DoesNotContain(defaultItems, item => item.Availability == "unavailable");
        Assert.Contains(defaultItems, item => item.Availability == "unresolved");
    }
}
