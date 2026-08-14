using GdCli.Database;

namespace GdCli.Tests.Database;

public sealed class AffixRepositoryTests
{
    [Fact]
    public void QueriesFilterCountPageAndFindInSql()
    {
        using var fixture = new TestDatabase();
        using var database = new CliDatabase(fixture.Path);
        var filter = new AffixFilter("Rare", "prefix", null, null, null);

        Assert.Equal(1, database.Affixes.Count(filter));
        Assert.Equal("Balanced", Assert.Single(database.Affixes.Load(filter, 0, 1)).Name);
        Assert.Equal(
            "records/affixes/a.dbr",
            database.Affixes.FindByRecordId("RECORDS/AFFIXES/A.DBR")?.RecordId);
    }

    [Fact]
    public void QueriesFilterByCompatibleItemClassInSql()
    {
        using var fixture = new TestDatabase();
        using var database = new CliDatabase(fixture.Path);

        var result = database.Affixes.Load(
            new AffixFilter(null, null, "Mace", null, null),
            0,
            null);

        Assert.Equal("Balanced", Assert.Single(result).Name);
    }
}
