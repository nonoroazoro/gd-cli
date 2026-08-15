using GdCli.Contracts;
using System.Globalization;
using GdCli.Database;
using GdCli.Features.Acquisition;
using Microsoft.Data.Sqlite;

namespace GdCli.Tests.Database;

public sealed class AcquisitionCatalogBuilderTests
{
    [Fact]
    public void BuildDerivesFixedContainerRoutesAndLootChance()
    {
        using var fixture = new TestDatabase();
        fixture.Execute("""
            INSERT INTO field_names(id, name) VALUES
                (20, 'loot5Name1'),
                (21, 'lootTable'),
                (22, 'loot5Chance'),
                (23, 'lootName1');
            INSERT INTO records(id, record_id, class, template, display_name) VALUES
                (20, 'records/items/lootchests/chestloottables/specific.dbr', 'FixedItemLoot', 'database/templates/fixeditemloot.tpl', NULL),
                (21, 'records/items/lootchests/specific.dbr', 'FixedItemContainer', 'database/templates/fixeditemcontainer.tpl', 'Specific Chest'),
                (22, 'records/items/loottables/mastertables/specific.dbr', 'LootMasterTable', 'database/templates/lootmastertable.tpl', NULL);
            INSERT INTO record_references(source_pk, field_pk, ordinal, target_pk) VALUES
                (20, 20, 4, 22),
                (22, 23, 0, 2),
                (21, 21, 0, 20);
            INSERT INTO loot_conditions(record_pk, field_pk, ordinal, numeric_value)
            VALUES (20, 22, 4, 100);
            INSERT INTO levels(id, source_name, level_path, rift_gate_record_id)
            VALUES (1, 'base', 'world/chest', 'records/rift.dbr');
            INSERT INTO placements(level_pk, entity_ordinal, record_pk, world_x, world_y, world_z)
            VALUES (1, 0, 21, 1, 2, 3);
            """);

        fixture.Execute(AcquisitionCatalogBuilder.Build);
        fixture.Execute(AcquisitionGraphPruner.Prune);

        using var database = new CliDatabase(fixture.Path);
        var item = _item(database, "records/items/b.dbr");
        var methods = new AcquisitionResolver(database.Acquisitions).Resolve([item])[item.RecordId];
        var container = Assert.Single(methods, method => method.Kind == "container");
        Assert.DoesNotContain(methods, method => method.Kind == "randomDrop");
        Assert.Equal("Specific Chest", Assert.Single(container.Entities ?? []).Name);
        var route = Assert.Single(container.Routes ?? []);
        Assert.Equal(
            [
                "records/items/loottables/mastertables/specific.dbr",
                "records/items/lootchests/chestloottables/specific.dbr",
                "records/items/lootchests/specific.dbr"
            ],
            route.Path.Select(step => step.RecordId));
        Assert.Equal(100, Assert.Single(route.Path[1].Conditions).Value);
    }

    [Fact]
    public void BuildDoesNotExposeGenericRandomChestsAsSpecificContainers()
    {
        using var fixture = new TestDatabase();
        fixture.Execute("""
            INSERT INTO field_names(id, name) VALUES
                (20, 'lootName1'),
                (21, 'lootTable');
            INSERT INTO records(id, record_id, class, template, display_name) VALUES
                (20, 'records/items/loottables/mastertables/random.dbr', 'LootMasterTable', 'database/templates/lootmastertable.tpl', NULL),
                (21, 'records/items/lootchests/random.dbr', 'FixedItemContainer', 'database/templates/fixeditemcontainer.tpl', 'Random Chest');
            INSERT INTO record_references(source_pk, field_pk, ordinal, target_pk) VALUES
                (20, 20, 0, 2),
                (21, 21, 0, 20);
            """);

        fixture.Execute(AcquisitionCatalogBuilder.Build);
        fixture.Execute(AcquisitionGraphPruner.Prune);

        using var database = new CliDatabase(fixture.Path);
        var item = _item(database, "records/items/b.dbr");
        var methods = new AcquisitionResolver(database.Acquisitions).Resolve([item])[item.RecordId];

        Assert.Equal(["randomDrop"], methods.Select(method => method.Kind));
    }

    [Fact]
    public void BuildPreservesAContainerSpecificBranchForARandomDropItem()
    {
        using var fixture = new TestDatabase();
        fixture.Execute("""
            INSERT INTO field_names(id, name) VALUES
                (20, 'lootName1'),
                (21, 'lootTable');
            INSERT INTO records(id, record_id, class, template, display_name) VALUES
                (20, 'records/items/loottables/mastertables/random.dbr', 'LootMasterTable', 'database/templates/lootmastertable.tpl', NULL),
                (21, 'records/items/lootchests/specific-table.dbr', 'FixedItemLoot', 'database/templates/fixeditemloot.tpl', NULL),
                (22, 'records/items/lootchests/specific.dbr', 'FixedItemContainer', 'database/templates/fixeditemcontainer.tpl', 'Specific Chest');
            INSERT INTO record_references(source_pk, field_pk, ordinal, target_pk) VALUES
                (20, 20, 0, 2),
                (21, 20, 0, 2),
                (22, 21, 0, 21);
            """);

        fixture.Execute(AcquisitionCatalogBuilder.Build);
        fixture.Execute(AcquisitionGraphPruner.Prune);

        using var database = new CliDatabase(fixture.Path);
        var item = _item(database, "records/items/b.dbr");
        var methods = new AcquisitionResolver(database.Acquisitions).Resolve([item])[item.RecordId];

        Assert.Equal(["container", "randomDrop"], methods.Select(method => method.Kind).Order());
        Assert.Equal("Specific Chest", Assert.Single(
            Assert.Single(methods, method => method.Kind == "container").Entities ?? []).Name);
    }

    [Fact]
    public void BuildPreservesMiTraversalSemantics()
    {
        using var fixture = new TestDatabase();
        fixture.Execute("""
            UPDATE items SET is_mi = 0;
            UPDATE records
            SET record_id = 'records/items/gearweapons/test.dbr', class = 'WeaponMelee_Mace'
            WHERE id = 2;
            INSERT INTO field_names(id, name) VALUES
                (1, 'lootName1'),
                (2, 'lootName2'),
                (3, 'lootHeadItem1');
            INSERT INTO records(id, record_id, class, template, display_name) VALUES
                (7, 'records/items/loottables/a.dbr', 'LootTable', '', ''),
                (8, 'records/items/loottables/b.dbr', 'LootTable', '', ''),
                (9, 'records/creatures/monster.dbr', 'Monster', 'database/templates/monster.tpl', 'Monster');
            INSERT INTO record_references(source_pk, field_pk, ordinal, target_pk) VALUES
                (7, 1, 0, 2),
                (8, 1, 0, 7),
                (7, 2, 0, 8),
                (9, 3, 0, 8),
                (9, 3, 1, 1);
            """);

        fixture.Execute(AcquisitionCatalogBuilder.Build);

        using var connection = _openReadOnly(fixture.Path);
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT I.is_mi, S.record_id
            FROM items I
            JOIN acquisition_sources A ON A.item_pk = I.record_pk
            JOIN records S ON S.id = A.source_pk
            WHERE I.record_pk = 2 AND A.kind = 'specificMonster'
            """;
        using var reader = command.ExecuteReader();
        Assert.True(reader.Read());
        Assert.True(reader.GetBoolean(0));
        Assert.Equal("records/creatures/monster.dbr", reader.GetString(1));
        Assert.False(reader.Read());
        reader.Close();
        command.CommandText = """
            SELECT COUNT(*)
            FROM acquisition_sources
            WHERE item_pk = 1 AND kind = 'specificMonster'
            """;
        Assert.Equal(0L, command.ExecuteScalar());
    }

    [Fact]
    public void BuildDerivesCraftVendorAndRandomSources()
    {
        using var fixture = new TestDatabase();
        fixture.Execute("""
            UPDATE items SET is_mi = 0;
            INSERT INTO field_names(id, name) VALUES
                (1, 'artifactName'),
                (2, 'marketFileName'),
                (3, 'marketWaistTable'),
                (4, 'records'),
                (5, 'lootName1'),
                (6, 'lootMisc1Item1'),
                (7, 'marketWeaponTable');
            INSERT INTO records(id, record_id, class, template, name_tag, display_name) VALUES
                (7, 'records/items/blueprint.dbr', 'ItemArtifactFormula', '', 'tagBlueprint', 'Blueprint'),
                (8, 'records/creatures/npcs/merchant.dbr', 'NpcMerchant', '', 'tagMerchant', 'Merchant'),
                (9, 'records/creatures/npcs/market.dbr', '', 'database/templates/market.tpl', NULL, 'Market'),
                (10, 'records/items/loottables/vendor.dbr', 'LootTable', '', NULL, 'Vendor table'),
                (11, 'records/items/loottables/mastertables/random.dbr', 'LootMasterTable', 'database/templates/lootmastertable.tpl', NULL, 'Random table'),
                (12, 'records/items/loottables/random.dbr', 'LootTable', '', NULL, 'Random items'),
                (13, 'records/creatures/monster.dbr', 'Monster', 'database/templates/monster.tpl', 'tagMonster', 'Monster'),
                (14, 'records/items/loottables/recipe.dbr', 'LootTable', '', NULL, 'Recipe table');
            INSERT INTO items(record_pk, rarity, item_class, item_level, required_level, is_mi)
            VALUES (7, 'Legendary', 'ItemArtifactFormula', 1, 0, 0);
            INSERT INTO record_references(source_pk, field_pk, ordinal, target_pk) VALUES
                (7, 1, 0, 14),
                (14, 4, 0, 2),
                (8, 2, 0, 9),
                (9, 3, 0, 10),
                (9, 7, 0, 1),
                (10, 5, 0, 7),
                (11, 5, 0, 12),
                (12, 4, 0, 7),
                (13, 6, 0, 11),
                (13, 6, 1, 7);
            """);

        fixture.Execute(AcquisitionCatalogBuilder.Build);

        using var connection = _openReadOnly(fixture.Path);
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT kind, COALESCE(S.record_id, '')
            FROM acquisition_sources A
            LEFT JOIN records S ON S.id = A.source_pk
            WHERE A.item_pk = 7
            ORDER BY kind
            """;
        using var reader = command.ExecuteReader();
        var sources = new List<(string Kind, string RecordId)>();
        while (reader.Read())
            sources.Add((reader.GetString(0), reader.GetString(1)));
        Assert.Equal(
        [
            ("randomDrop", ""),
            ("specificMonster", "records/creatures/monster.dbr"),
            ("vendor", "records/creatures/npcs/merchant.dbr")
        ], sources);
        reader.Close();

        command.CommandText = "SELECT recipe_item_pk FROM recipes WHERE result_item_pk = 2";
        Assert.Equal(7L, command.ExecuteScalar());
        command.CommandText = """
            SELECT S.record_id
            FROM acquisition_sources A
            JOIN records S ON S.id = A.source_pk
            WHERE A.item_pk = 1 AND A.kind = 'vendor'
            """;
        Assert.Equal("records/creatures/npcs/merchant.dbr", command.ExecuteScalar());
        command.CommandText = "SELECT is_mi FROM items WHERE record_pk = 7";
        Assert.False(Convert.ToBoolean(command.ExecuteScalar(), CultureInfo.InvariantCulture));
    }

    [Fact]
    public void BuildPreservesOnlyTheGraphRequiredByAcquisitionQueries()
    {
        using var fixture = new TestDatabase();
        fixture.Execute("""
            UPDATE items SET is_mi = 0;
            UPDATE records
            SET record_id = 'records/items/gearweapons/test.dbr', class = 'WeaponMelee_Mace'
            WHERE id = 2;
            INSERT INTO field_names(id, name) VALUES
                (1, 'lootName1'),
                (2, 'lootHeadItem1'),
                (3, 'pool1'),
                (4, 'marketFileName'),
                (5, 'marketWaistTable'),
                (6, 'unused');
            INSERT INTO records(id, record_id, class, template, name_tag, display_name) VALUES
                (7, 'records/items/loottables/specific.dbr', 'LootTable', '', NULL, 'Specific table'),
                (8, 'records/creatures/monster.dbr', 'Monster', 'database/templates/monster.tpl', 'tagMonster', 'Monster'),
                (9, 'records/proxies/monster.dbr', 'Proxy', '', NULL, 'Monster proxy'),
                (10, 'records/creatures/npcs/merchant.dbr', 'NpcMerchant', '', 'tagMerchant', 'Merchant'),
                (11, 'records/creatures/npcs/market.dbr', '', 'database/templates/market.tpl', NULL, 'Market'),
                (12, 'records/items/loottables/vendor.dbr', 'LootTable', '', NULL, 'Vendor table'),
                (13, 'records/proxies/merchant.dbr', 'Proxy', '', NULL, 'Merchant proxy'),
                (14, 'records/unused/source.dbr', '', '', NULL, 'Unused source'),
                (15, 'records/unused/target.dbr', '', '', NULL, 'Unused target');
            INSERT INTO record_references(source_pk, field_pk, ordinal, target_pk) VALUES
                (7, 1, 0, 2),
                (8, 2, 0, 7),
                (9, 3, 0, 8),
                (10, 4, 0, 11),
                (11, 5, 0, 12),
                (12, 1, 0, 2),
                (13, 3, 0, 10),
                (14, 6, 0, 15);
            """);

        fixture.Execute(AcquisitionCatalogBuilder.Build);
        fixture.Execute("""
            INSERT INTO levels(id, source_name, level_path, rift_gate_record_id) VALUES
                (1, 'base', 'world/monster', 'records/rift.dbr'),
                (2, 'base', 'world/vendor', 'records/rift.dbr');
            INSERT INTO placements(level_pk, entity_ordinal, record_pk, world_x, world_y, world_z) VALUES
                (1, 0, 9, 1, 2, 3),
                (2, 0, 13, 4, 5, 6);
            """);
        fixture.Execute(AcquisitionGraphPruner.Prune);

        using var database = new CliDatabase(fixture.Path);
        var item = _item(database, "records/items/gearweapons/test.dbr");
        var methods = new AcquisitionResolver(database.Acquisitions).Resolve([item])[item.RecordId];
        var vendor = Assert.Single(methods, method => method.Kind == "vendor");
        var monster = Assert.Single(methods, method => method.Kind == "specificMonster");

        Assert.Equal("world/vendor", Assert.Single(Assert.Single(vendor.Entities ?? []).Locations).Level);
        Assert.Equal(
            [
                "records/items/loottables/specific.dbr",
                "records/creatures/monster.dbr",
                "records/proxies/monster.dbr"
            ],
            Assert.Single(monster.Routes ?? []).Path.Select(step => step.RecordId));
        using var connection = _openReadOnly(fixture.Path);
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM record_references WHERE source_pk = 14";
        Assert.Equal(0L, command.ExecuteScalar());
    }

    private static SqliteConnection _openReadOnly(string path)
    {
        var connection = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = path,
            Mode = SqliteOpenMode.ReadOnly,
            Pooling = false
        }.ToString());
        connection.Open();
        return connection;
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
