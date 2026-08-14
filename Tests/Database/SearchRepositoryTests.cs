using GdCli.Database;

namespace GdCli.Tests.Database;

public sealed class SearchRepositoryTests
{
    [Fact]
    public void QueriesMergeAndPageInSql()
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
    public void TreatsLikeWildcardsAsLiteralCharacters()
    {
        using var fixture = new TestDatabase();
        using var database = new CliDatabase(fixture.Path);
        var filter = new SearchFilter("100%", null, null, null, null, null);

        Assert.Equal(1, database.Search.Count(filter));
        Assert.Equal("100% Blade", Assert.Single(database.Search.Load(filter, 0, null)).Name);
    }
}
