using GdCli.Database;

namespace GdCli.Tests.Database;

public sealed class ItemRepositoryTests
{
    [Fact]
    public void QueriesFilterCountPageAndFindInSql()
    {
        using var fixture = new TestDatabase();
        using var database = new CliDatabase(fixture.Path);
        var filter = new ItemFilter("Rare", "Mace", 0, 100, null);

        Assert.Equal(2, database.Items.Count(filter));
        var item = Assert.Single(database.Items.Load(filter, 1, 1));
        Assert.Equal("records/items/c.dbr", item.RecordId);
        Assert.Equal("tagShared", item.NameTag);
        Assert.Equal(
            "records/items/b.dbr",
            database.Items.FindByRecordId("RECORDS/ITEMS/B.DBR")?.RecordId);
        Assert.Null(database.Items.FindByRecordId("records/items/percent.dbr")?.NameTag);
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
    public void DropCandidatesKeepExactAndMiSemanticsInSql()
    {
        using var fixture = new TestDatabase();
        using var database = new CliDatabase(fixture.Path);

        Assert.Equal(1, database.Items.CountMatches("Beta", true, false));
        Assert.Equal(1, database.Items.CountMatches("Beta", true, true));
        Assert.Equal(
            "records/items/b.dbr",
            Assert.Single(database.Items.LoadMatches("Beta", true, true, 0, 1)).RecordId);
        Assert.Equal(1, database.Items.CountMatches("Alpi", false, false));
        Assert.Equal(0, database.Items.CountMatches("Alpi", false, true));
    }
}
