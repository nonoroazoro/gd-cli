using GdCli.Application;
using GdCli.Commands;
using GdCli.Contracts;
using GdCli.Database;
using GdCli.Tests.Database;

namespace GdCli.Tests.Application;

public sealed class ItemsCommandTests
{
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
        var options = CommandLineParser.Parse(["items", "Beta", "--all", "--no-stats"]);
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
