using GdCli.Database;

namespace GdCli.Tests.Database;

public sealed class CliDatabaseTests
{
    [Fact]
    public void ItemQueriesFilterCountPageAndFindInSql()
    {
        using var fixture = new TestDatabase();
        using var database = new CliDatabase(fixture.Path);
        var filter = new ItemFilter("Rare", "Mace", 0, 100);

        Assert.Equal(2, database.Items.Count(filter));
        var page = database.Items.Load(filter, 1, 1);
        var item = Assert.Single(page);
        Assert.Equal("records/items/c.dbr", item.RecordId);
        Assert.Equal("records/items/b.dbr", database.Items.FindByRecordId("RECORDS/ITEMS/B.DBR")?.RecordId);
    }

    [Fact]
    public void AffixQueriesFilterCountPageAndFindInSql()
    {
        using var fixture = new TestDatabase();
        using var database = new CliDatabase(fixture.Path);
        var filter = new AffixFilter("Rare", "prefix", null, null);

        Assert.Equal(1, database.Affixes.Count(filter));
        Assert.Equal("Balanced", Assert.Single(database.Affixes.Load(filter, 0, 1)).Name);
        Assert.Equal("records/affixes/a.dbr", database.Affixes.FindByRecordId("RECORDS/AFFIXES/A.DBR")?.RecordId);
    }

    [Fact]
    public void SearchQueriesMergeAndPageInSql()
    {
        using var fixture = new TestDatabase();
        using var database = new CliDatabase(fixture.Path);
        var filter = new SearchFilter("al", null, null, null, null, null);

        Assert.Equal(3, database.Search.Count(filter));
        var hit = Assert.Single(database.Search.Load(filter, 1, 1));
        Assert.Equal("item", hit.Entity);
        Assert.Equal("records/items/a.dbr", hit.RecordId);
    }

    [Fact]
    public void SearchTreatsLikeWildcardsAsLiteralCharacters()
    {
        using var fixture = new TestDatabase();
        using var database = new CliDatabase(fixture.Path);
        var filter = new SearchFilter("100%", null, null, null, null, null);

        Assert.Equal(1, database.Search.Count(filter));
        Assert.Equal("100% Blade", Assert.Single(database.Search.Load(filter, 0, null)).Name);
    }

    [Fact]
    public void DropCandidatesKeepExactAndMiSemanticsInSql()
    {
        using var fixture = new TestDatabase();
        using var database = new CliDatabase(fixture.Path);

        Assert.Equal(1, database.Items.CountMatches("Beta", true, false));
        Assert.Equal(1, database.Items.CountMatches("Beta", true, true));
        Assert.Equal("records/items/b.dbr", Assert.Single(database.Items.LoadMatches("Beta", true, true, 0, 1)).RecordId);
        Assert.Equal(1, database.Items.CountMatches("Alpi", false, false));
        Assert.Equal(0, database.Items.CountMatches("Alpi", false, true));
    }
}
