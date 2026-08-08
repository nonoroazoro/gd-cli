using GdCli.Database;

namespace GdCli.Tests.Database;

public sealed class AffixCompatibilityBuilderTests
{
    [Fact]
    public void BuildDerivesItemClassCompatibilityFromLootTables()
    {
        using var fixture = new TestDatabase();
        fixture.Execute((connection, transaction) =>
        {
            using var setup = connection.CreateCommand();
            setup.Transaction = transaction;
            setup.CommandText = """
                DELETE FROM affix_item_classes;
                INSERT INTO records(id, record_id, source_name, class, display_name) VALUES
                    (20, 'records/items/loottables/dynamic.dbr', 'base', 'LootItemTable_DynWeight', 'dynamic'),
                    (21, 'records/items/lootaffixes/prefix/table.dbr', 'base', 'LootRandomizerTable', 'table');
                INSERT INTO field_names(id, name) VALUES
                    (20, 'lootName1'),
                    (21, 'prefixTableName1'),
                    (22, 'randomizerName1');
                INSERT INTO record_references(source_pk, field_pk, ordinal, target_pk) VALUES
                    (20, 20, 0, 2),
                    (20, 21, 0, 21),
                    (21, 22, 0, 5);
                """;
            setup.ExecuteNonQuery();
            AffixCompatibilityBuilder.Build(connection, transaction);
        });

        using var database = new CliDatabase(fixture.Path);
        var result = database.Affixes.Load(
            new AffixFilter(null, null, "Mace", null, null),
            0,
            null);

        Assert.Equal("Balanced", Assert.Single(result).Name);
    }
}
