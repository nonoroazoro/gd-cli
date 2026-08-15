using GdCli.Database;

namespace GdCli.Tests.Database;

public sealed class GameRecordCatalogBuilderTests
{
    [Fact]
    public void BuildExcludesLootRandomizerTablesFromAffixes()
    {
        using var fixture = new TestDatabase();
        fixture.Execute((connection, transaction) =>
        {
            using var setup = connection.CreateCommand();
            setup.Transaction = transaction;
            setup.CommandText = """
                DELETE FROM affix_item_classes;
                DELETE FROM affixes;
                DELETE FROM items;
                UPDATE records
                SET class = 'LootRandomizer',
                    record_id = CASE id
                        WHEN 5 THEN 'records/items/lootaffixes/prefix/a.dbr'
                        ELSE 'records/items/lootaffixes/suffix/b.dbr'
                    END
                WHERE id IN (5, 6);
                INSERT INTO records(id, record_id, class, display_name) VALUES
                    (20, 'records/items/lootaffixes/prefix/table.dbr', 'LootRandomizerTable', 'table'),
                    (21, 'records/items/lootaffixes/suffix/table.dbr', 'LootRandomizerTable', 'table');
                """;
            setup.ExecuteNonQuery();
            GameRecordCatalogBuilder.Build(connection, transaction);
        });

        using var database = new CliDatabase(fixture.Path);
        var affixes = database.Affixes.Load(
            new AffixFilter(null, null, null, null, null, null, null),
            0,
            null);

        Assert.Equal(2, affixes.Count);
        Assert.DoesNotContain(affixes, affix => affix.RecordId.EndsWith("table.dbr", StringComparison.Ordinal));
    }
}
