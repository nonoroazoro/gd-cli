using GdCli.Database;
using GdCli.Features.Drops;
using GdCli.Tests.Database;

namespace GdCli.Tests.Features.Drops;

public sealed class DropResolverTests
{
    [Fact]
    public void ResolveReturnsMiSourceWithoutFixedMapLocation()
    {
        using var fixture = new TestDatabase();
        fixture.Execute("""
            INSERT INTO records(id, record_id, source_name, class, display_name)
            VALUES (7, 'records/creatures/monster.dbr', 'base', 'Monster', 'Monster');
            INSERT INTO monster_drops(item_pk, monster_pk) VALUES (2, 7);
            """);
        using var database = new CliDatabase(fixture.Path);
        var item = database.Items.FindByRecordId("records/items/b.dbr") ?? throw new InvalidOperationException();

        var result = Assert.Single(new DropResolver(database).Resolve([item]));

        Assert.Equal("records/creatures/monster.dbr", Assert.Single(result.MiSources).RecordId);
        Assert.Empty(result.Routes);
        Assert.False(result.RoutesTruncated);
    }

    [Fact]
    public void ResolveCountsOnlyDistinctRoutesAgainstLimit()
    {
        using var fixture = new TestDatabase();
        fixture.Execute($"""
            INSERT INTO field_names(id, name) VALUES (1, 'lootItem1');
            INSERT INTO records(id, record_id, source_name, class, display_name)
            VALUES (7, 'records/creatures/monster.dbr', 'base', 'Monster', 'Monster');
            INSERT INTO record_references(source_pk, field_pk, ordinal, target_pk) VALUES (7, 1, 0, 2);
            INSERT INTO monster_drops(item_pk, monster_pk) VALUES (2, 7);
            INSERT INTO levels(id, source_name, level_path, rift_gate_record_id, offset_x, offset_y, offset_z)
            VALUES (1, 'base', 'world/test', '', 0, 0, 0);
            {_placements(513, false)}
            """);
        using var database = new CliDatabase(fixture.Path);
        var item = database.Items.FindByRecordId("records/items/b.dbr") ?? throw new InvalidOperationException();

        var result = Assert.Single(new DropResolver(database).Resolve([item]));

        Assert.Single(result.Routes);
        Assert.False(result.RoutesTruncated);
    }

    [Fact]
    public void ResolveReportsDistinctRouteLimitTruncation()
    {
        using var fixture = new TestDatabase();
        fixture.Execute($"""
            INSERT INTO field_names(id, name) VALUES (1, 'lootItem1');
            INSERT INTO records(id, record_id, source_name, class, display_name)
            VALUES (7, 'records/creatures/monster.dbr', 'base', 'Monster', 'Monster');
            INSERT INTO record_references(source_pk, field_pk, ordinal, target_pk) VALUES (7, 1, 0, 2);
            INSERT INTO monster_drops(item_pk, monster_pk) VALUES (2, 7);
            INSERT INTO levels(id, source_name, level_path, rift_gate_record_id, offset_x, offset_y, offset_z)
            VALUES (1, 'base', 'world/test', '', 0, 0, 0);
            {_placements(513, true)}
            """);
        using var database = new CliDatabase(fixture.Path);
        var item = database.Items.FindByRecordId("records/items/b.dbr") ?? throw new InvalidOperationException();

        var result = Assert.Single(new DropResolver(database).Resolve([item]));

        Assert.Equal(512, result.Routes.Count);
        Assert.True(result.RoutesTruncated);
        Assert.Equal(512, result.RouteLimit);
        Assert.Equal(8, result.MaximumDepth);
    }

    [Fact]
    public void ResolveReportsDepthTruncation()
    {
        using var fixture = new TestDatabase();
        var records = string.Join(',', Enumerable.Range(0, 9).Select(index =>
            $"({7 + index}, 'records/items/loottables/{index}.dbr', 'base', 'LootTable', '')"));
        var references = string.Join(',', Enumerable.Range(0, 9).Select(index =>
            $"({7 + index}, 1, 0, {(index == 0 ? 2 : 6 + index)})"));
        fixture.Execute($"""
            INSERT INTO field_names(id, name) VALUES (1, 'lootName1');
            INSERT INTO records(id, record_id, source_name, class, display_name) VALUES {records};
            INSERT INTO record_references(source_pk, field_pk, ordinal, target_pk) VALUES {references};
            """);
        using var database = new CliDatabase(fixture.Path);
        var item = database.Items.FindByRecordId("records/items/b.dbr") ?? throw new InvalidOperationException();

        var result = Assert.Single(new DropResolver(database).Resolve([item]));

        Assert.Empty(result.Routes);
        Assert.True(result.RoutesTruncated);
    }

    private static string _placements(int count, bool unique)
    {
        var values = string.Join(',', Enumerable.Range(0, count).Select(index =>
            $"(1, {index}, 7, {(unique ? index : 1)}, 2, 3)"));
        return $"INSERT INTO placements(level_pk, entity_ordinal, record_pk, world_x, world_y, world_z) VALUES {values};";
    }
}
