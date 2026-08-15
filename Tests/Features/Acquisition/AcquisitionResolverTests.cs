using GdCli.Contracts;
using GdCli.Database;
using GdCli.Features.Acquisition;
using GdCli.Tests.Database;

namespace GdCli.Tests.Features.Acquisition;

public sealed class AcquisitionResolverTests
{
    [Fact]
    public void ResolveReturnsUnknownWhenNoSourceIsKnown()
    {
        using var fixture = new TestDatabase();
        using var database = new CliDatabase(fixture.Path);
        var item = _item(database, "records/items/a.dbr");

        var methods = new AcquisitionResolver(database.Acquisitions).Resolve([item])[item.RecordId];

        Assert.Equal("unknown", Assert.Single(methods).Kind);
    }

    [Fact]
    public void ResolveGroupsActorVariantsAndResolvesProxyLocation()
    {
        using var fixture = new TestDatabase();
        fixture.Execute("""
            INSERT INTO field_names(id, name) VALUES
                (1, 'lootHeadItem1'),
                (2, 'pool1');
            INSERT INTO records(id, record_id, class, name_tag, display_name) VALUES
                (7, 'records/creatures/monster_a.dbr', 'Monster', 'tagMonster', 'Monster'),
                (8, 'records/creatures/monster_b.dbr', 'Monster', 'tagMonster', 'Monster'),
                (9, 'records/proxies/monster.dbr', 'Proxy', NULL, 'Proxy');
            INSERT INTO acquisition_sources(item_pk, kind, source_pk) VALUES
                (2, 'specificMonster', 7),
                (2, 'specificMonster', 8);
            INSERT INTO record_references(source_pk, field_pk, ordinal, target_pk) VALUES
                (7, 1, 0, 2),
                (9, 2, 0, 7),
                (9, 2, 1, 8);
            INSERT INTO levels(id, source_name, level_path, rift_gate_record_id)
            VALUES (1, 'base', 'world/proxy', 'records/rift.dbr');
            INSERT INTO placements(level_pk, entity_ordinal, record_pk, world_x, world_y, world_z)
            VALUES (1, 0, 9, 1, 2, 3);
            """);
        using var database = new CliDatabase(fixture.Path);
        var item = _item(database, "records/items/b.dbr");

        var methods = new AcquisitionResolver(database.Acquisitions).Resolve([item])[item.RecordId];
        var method = Assert.Single(methods);
        var actor = Assert.Single(method.Actors ?? []);
        var route = Assert.Single(method.Routes ?? []);

        Assert.Equal(2, actor.RecordIds.Count);
        Assert.Equal("records/proxies/monster.dbr", Assert.Single(actor.Locations).PlacedRecordId);
        Assert.Equal(
            ["records/creatures/monster_a.dbr", "records/proxies/monster.dbr"],
            route.Path.Select(step => step.RecordId));
    }

    [Fact]
    public void ResolveKeepsActorsWithoutStableTagsSeparate()
    {
        using var fixture = new TestDatabase();
        fixture.Execute("""
            INSERT INTO records(id, record_id, class, display_name) VALUES
                (7, 'records/creatures/a.dbr', 'Monster', 'Shared name'),
                (8, 'records/creatures/b.dbr', 'Monster', 'Shared name'),
                (9, 'records/creatures/c.dbr', 'Monster', ''),
                (10, 'records/creatures/d.dbr', 'Monster', '');
            INSERT INTO acquisition_sources(item_pk, kind, source_pk) VALUES
                (2, 'specificMonster', 7),
                (2, 'specificMonster', 8),
                (2, 'specificMonster', 9),
                (2, 'specificMonster', 10);
            """);
        using var database = new CliDatabase(fixture.Path);
        var item = _item(database, "records/items/b.dbr");

        var methods = new AcquisitionResolver(database.Acquisitions).Resolve([item])[item.RecordId];
        var actors = Assert.Single(methods).Actors ?? [];

        Assert.Equal(4, actors.Count);
        Assert.All(actors, actor => Assert.Single(actor.RecordIds));
        Assert.Equal(
            [
                "records/creatures/a.dbr",
                "records/creatures/b.dbr",
                "records/creatures/c.dbr",
                "records/creatures/d.dbr"
            ],
            actors.SelectMany(actor => actor.RecordIds).Order(StringComparer.OrdinalIgnoreCase));
    }

    [Fact]
    public void ResolveReturnsDirectVendorWithLocation()
    {
        using var fixture = new TestDatabase();
        fixture.Execute("""
            INSERT INTO records(id, record_id, class, name_tag, display_name)
            VALUES (7, 'records/creatures/npcs/merchant.dbr', 'NpcMerchant', 'tagMerchant', 'Merchant');
            INSERT INTO acquisition_sources(item_pk, kind, source_pk) VALUES (2, 'vendor', 7);
            INSERT INTO levels(id, source_name, level_path, rift_gate_record_id)
            VALUES (1, 'base', 'world/vendor', 'records/rift.dbr');
            INSERT INTO placements(level_pk, entity_ordinal, record_pk, world_x, world_y, world_z)
            VALUES (1, 0, 7, 1, 2, 3);
            """);
        using var database = new CliDatabase(fixture.Path);
        var item = _item(database, "records/items/b.dbr");

        var methods = new AcquisitionResolver(database.Acquisitions).Resolve([item])[item.RecordId];
        var method = Assert.Single(methods);
        var actor = Assert.Single(method.Actors ?? []);

        Assert.Equal("vendor", method.Kind);
        Assert.Equal("records/creatures/npcs/merchant.dbr", Assert.Single(actor.RecordIds));
        Assert.Equal("world/vendor", Assert.Single(actor.Locations).Level);
    }

    [Fact]
    public void ResolveReturnsSpecificMonsterWithoutFixedMapLocation()
    {
        using var fixture = new TestDatabase();
        fixture.Execute("""
            INSERT INTO records(id, record_id, class, display_name)
            VALUES (7, 'records/creatures/monster.dbr', 'Monster', 'Monster');
            INSERT INTO acquisition_sources(item_pk, kind, source_pk)
            VALUES (2, 'specificMonster', 7);
            """);
        using var database = new CliDatabase(fixture.Path);
        var item = _item(database, "records/items/b.dbr");

        var methods = new AcquisitionResolver(database.Acquisitions).Resolve([item])[item.RecordId];
        var method = Assert.Single(methods);

        Assert.Equal("specificMonster", method.Kind);
        Assert.Equal(
            "records/creatures/monster.dbr",
            Assert.Single(Assert.Single(method.Actors ?? []).RecordIds));
        Assert.Empty(method.Routes ?? []);
        Assert.False(method.RoutesTruncated);
    }

    [Fact]
    public void ResolveCombinesDirectAndCraftSources()
    {
        using var fixture = new TestDatabase();
        fixture.Execute("""
            INSERT INTO records(id, record_id, class, name_tag, display_name) VALUES
                (7, 'records/items/blueprint.dbr', 'ItemArtifactFormula', 'tagBlueprint', 'Blueprint'),
                (8, 'records/creatures/npcs/merchant.dbr', 'NpcMerchant', 'tagMerchant', 'Merchant');
            INSERT INTO items(record_pk, rarity, item_class, item_level, required_level, is_mi)
            VALUES (7, 'Legendary', 'ItemArtifactFormula', 1, 0, 0);
            INSERT INTO recipes(result_item_pk, recipe_item_pk) VALUES (2, 7);
            INSERT INTO acquisition_sources(item_pk, kind, source_pk) VALUES
                (2, 'randomDrop', NULL),
                (7, 'vendor', 8),
                (7, 'randomDrop', NULL);
            INSERT INTO levels(id, source_name, level_path, rift_gate_record_id)
            VALUES (1, 'base', 'world/vendor', 'records/rift.dbr');
            INSERT INTO placements(level_pk, entity_ordinal, record_pk, world_x, world_y, world_z)
            VALUES (1, 0, 8, 1, 2, 3);
            """);
        using var database = new CliDatabase(fixture.Path);
        var item = _item(database, "records/items/b.dbr");

        var methods = new AcquisitionResolver(database.Acquisitions).Resolve([item])[item.RecordId];
        var craft = Assert.Single(methods, method => method.Kind == "craft");

        Assert.Equal(["randomDrop", "craft"], methods.Select(method => method.Kind));
        Assert.Equal("craft", craft.Kind);
        Assert.Equal("records/items/blueprint.dbr", craft.Recipe?.RecordId);
        Assert.Equal(["vendor", "randomDrop"], craft.Sources?.Select(source => source.Kind));
        var vendor = craft.Sources?[0] ?? throw new InvalidOperationException();
        Assert.Equal("world/vendor", Assert.Single(Assert.Single(vendor.Actors ?? []).Locations).Level);
    }

    [Fact]
    public void ResolvePreservesDistinctFieldsAlongTheSameRoute()
    {
        using var fixture = new TestDatabase();
        fixture.Execute("""
            INSERT INTO field_names(id, name) VALUES
                (1, 'lootName1'),
                (2, 'lootName2'),
                (3, 'lootHeadItem1');
            INSERT INTO records(id, record_id, class, display_name) VALUES
                (7, 'records/items/loottables/table.dbr', 'LootTable', 'Table'),
                (8, 'records/creatures/monster.dbr', 'Monster', 'Monster');
            INSERT INTO record_references(source_pk, field_pk, ordinal, target_pk) VALUES
                (7, 1, 0, 2),
                (7, 2, 0, 2),
                (8, 3, 0, 7);
            INSERT INTO acquisition_sources(item_pk, kind, source_pk)
            VALUES (2, 'specificMonster', 8);
            INSERT INTO levels(id, source_name, level_path, rift_gate_record_id)
            VALUES (1, 'base', 'world/test', '');
            INSERT INTO placements(level_pk, entity_ordinal, record_pk, world_x, world_y, world_z)
            VALUES (1, 0, 8, 1, 2, 3);
            """);
        using var database = new CliDatabase(fixture.Path);
        var item = _item(database, "records/items/b.dbr");

        var methods = new AcquisitionResolver(database.Acquisitions).Resolve([item])[item.RecordId];
        var routes = Assert.Single(methods).Routes ?? [];

        Assert.Equal(2, routes.Count);
        Assert.Equal(
            ["lootName1", "lootName2"],
            routes.Select(route => route.Path[0].Field).Order(StringComparer.Ordinal));
    }

    [Fact]
    public void ResolveCountsOnlyDistinctRoutesAgainstLimit()
    {
        using var fixture = new TestDatabase();
        fixture.Execute($"""
            INSERT INTO field_names(id, name) VALUES (1, 'lootItem1');
            INSERT INTO records(id, record_id, class, display_name)
            VALUES (7, 'records/creatures/monster.dbr', 'Monster', 'Monster');
            INSERT INTO record_references(source_pk, field_pk, ordinal, target_pk) VALUES (7, 1, 0, 2);
            INSERT INTO acquisition_sources(item_pk, kind, source_pk) VALUES (2, 'specificMonster', 7);
            INSERT INTO levels(id, source_name, level_path, rift_gate_record_id)
            VALUES (1, 'base', 'world/test', '');
            {_placements(513, false)}
            """);
        using var database = new CliDatabase(fixture.Path);
        var item = _item(database, "records/items/b.dbr");

        var methods = new AcquisitionResolver(database.Acquisitions).Resolve([item])[item.RecordId];
        var method = Assert.Single(methods);

        Assert.Single(method.Routes ?? []);
        Assert.False(method.RoutesTruncated);
    }

    [Fact]
    public void ResolveReportsRouteLimitTruncation()
    {
        using var fixture = new TestDatabase();
        fixture.Execute($"""
            INSERT INTO field_names(id, name) VALUES (1, 'lootItem1');
            INSERT INTO records(id, record_id, class, display_name)
            VALUES (7, 'records/creatures/monster.dbr', 'Monster', 'Monster');
            INSERT INTO record_references(source_pk, field_pk, ordinal, target_pk) VALUES (7, 1, 0, 2);
            INSERT INTO acquisition_sources(item_pk, kind, source_pk) VALUES (2, 'specificMonster', 7);
            INSERT INTO levels(id, source_name, level_path, rift_gate_record_id)
            VALUES (1, 'base', 'world/test', '');
            {_placements(513, true)}
            """);
        using var database = new CliDatabase(fixture.Path);
        var item = _item(database, "records/items/b.dbr");

        var methods = new AcquisitionResolver(database.Acquisitions).Resolve([item])[item.RecordId];
        var method = Assert.Single(methods);

        Assert.Equal(512, method.Routes?.Count);
        Assert.True(method.RoutesTruncated);
        Assert.Equal(512, method.RouteLimit);
        Assert.Equal(8, method.MaximumDepth);
    }

    [Fact]
    public void ResolveReportsDepthTruncation()
    {
        using var fixture = new TestDatabase();
        var records = string.Join(',', Enumerable.Range(0, 9).Select(index =>
            $"({7 + index}, 'records/items/loottables/{index}.dbr', 'LootTable', '')"));
        var references = string.Join(',', Enumerable.Range(0, 9).Select(index =>
            $"({7 + index}, 1, 0, {(index == 0 ? 2 : 6 + index)})"));
        fixture.Execute($"""
            INSERT INTO field_names(id, name) VALUES (1, 'lootName1');
            INSERT INTO records(id, record_id, class, display_name) VALUES {records};
            INSERT INTO record_references(source_pk, field_pk, ordinal, target_pk) VALUES {references};
            INSERT INTO acquisition_sources(item_pk, kind, source_pk) VALUES (2, 'specificMonster', 7);
            """);
        using var database = new CliDatabase(fixture.Path);
        var item = _item(database, "records/items/b.dbr");

        var methods = new AcquisitionResolver(database.Acquisitions).Resolve([item])[item.RecordId];
        var method = Assert.Single(methods);

        Assert.Empty(method.Routes ?? []);
        Assert.True(method.RoutesTruncated);
    }

    private static string _placements(int count, bool unique)
    {
        var values = string.Join(',', Enumerable.Range(0, count).Select(index =>
            $"(1, {index}, 7, {(unique ? index : 1)}, 2, 3)"));
        return $"INSERT INTO placements(level_pk, entity_ordinal, record_pk, world_x, world_y, world_z) VALUES {values};";
    }

    private static ItemRecord _item(CliDatabase database, string recordId)
    {
        var filter = new ItemFilter(
            null,
            null,
            null,
            null,
            null,
            IncludeUnavailable: true,
            Query: recordId,
            ExactQuery: true);
        return Assert.Single(database.Items.Load(filter, 0, 1));
    }
}
