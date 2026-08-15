using GdCli.Database;

namespace GdCli.Tests.Database;

public sealed class ItemRepositoryTests
{
    [Fact]
    public void QueriesFilterCountAndPageInSql()
    {
        using var fixture = new TestDatabase();
        using var database = new CliDatabase(fixture.Path);
        var filter = new ItemFilter("Rare", "Mace", 0, 100, null);

        Assert.Equal(2, database.Items.Count(filter));
        var item = Assert.Single(database.Items.Load(filter, 1, 1));
        Assert.Equal("records/items/c.dbr", item.RecordId);
        Assert.Equal("tagShared", item.NameTag);
    }

    [Fact]
    public void QueriesFilterMiRecordsInSql()
    {
        using var fixture = new TestDatabase();
        using var database = new CliDatabase(fixture.Path);

        var miItems = database.Items.Load(new ItemFilter(null, null, null, null, true), 0, null);
        var nonMiItems = database.Items.Load(new ItemFilter(null, null, null, null, false), 0, null);

        Assert.Equal("records/items/b.dbr", Assert.Single(miItems).RecordId);
        Assert.Equal(3, nonMiItems.Count);
    }

    [Fact]
    public void QueriesMatchNamesWithExactAndMiSemantics()
    {
        using var fixture = new TestDatabase();
        using var database = new CliDatabase(fixture.Path);

        var exact = new ItemFilter(null, null, null, null, null, Query: "Beta", ExactQuery: true);
        var exactMi = exact with { IsMi = true };
        var partial = exact with { Query = "Alpi", ExactQuery = false };
        var literalWildcard = exact with { Query = "100%", ExactQuery = false };

        Assert.Equal(1, database.Items.Count(exact));
        Assert.Equal(1, database.Items.Count(exactMi));
        Assert.Equal(
            "records/items/b.dbr",
            Assert.Single(database.Items.Load(exactMi, 0, 1)).RecordId);
        Assert.Equal(1, database.Items.Count(partial));
        Assert.Equal(0, database.Items.Count(partial with { IsMi = true }));
        Assert.Equal(
            "records/items/percent.dbr",
            Assert.Single(database.Items.Load(literalWildcard, 0, null)).RecordId);
    }
}
