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
                INSERT INTO records(id, record_id, class, display_name) VALUES
                    (20, 'records/items/loottables/dynamic.dbr', 'LootItemTable_DynWeight', 'dynamic'),
                    (21, 'records/items/lootaffixes/prefix/table.dbr', 'LootRandomizerTable', 'table'),
                    (22, 'records/items/lootaffixes/prefix/other-table.dbr', 'LootRandomizerTable', 'other table');
                INSERT INTO field_names(id, name) VALUES
                    (20, 'lootName1'),
                    (21, 'prefixTableName1'),
                    (22, 'randomizerName1'),
                    (23, 'lootName2'),
                    (24, 'prefixTableName2');
                INSERT INTO record_references(source_pk, field_pk, ordinal, target_pk) VALUES
                    (20, 20, 0, 2),
                    (20, 21, 0, 21),
                    (20, 23, 0, 1),
                    (20, 24, 0, 22),
                    (21, 22, 0, 5),
                    (21, 22, 1, 900),
                    (22, 22, 0, 6);
                """;
            setup.ExecuteNonQuery();
            AffixCompatibilityBuilder.Build(connection, transaction);

            using var verify = connection.CreateCommand();
            verify.Transaction = transaction;
            verify.CommandText = "SELECT COUNT(*) FROM affix_item_classes WHERE affix_pk = 900";
            Assert.Equal(0L, verify.ExecuteScalar());
        });

        using var database = new CliDatabase(fixture.Path);
        var result = database.Affixes.Load(
            new AffixFilter(null, null, null, "Mace", null, null, null),
            0,
            null);

        Assert.Equal("Balanced", Assert.Single(result).Name);
    }
}
