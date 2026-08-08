using GdCli.Database;

namespace GdCli.Tests.Database;

public sealed class ItemFamilyRepositoryTests
{
    [Fact]
    public void PreservesMixedMiRecordState()
    {
        using var fixture = new TestDatabase();
        using var database = new CliDatabase(fixture.Path);

        Assert.Equal(3, database.ItemFamilies.Count(new ItemFamilyFilter(null)));
        var family = Assert.Single(database.ItemFamilies.Load(new ItemFamilyFilter(true), 0, null));

        Assert.Equal("tagShared", family.NameTag);
        Assert.True(family.HasMiRecord);
        Assert.True(family.HasNonMiRecord);
        Assert.Equal(["records/items/b.dbr", "records/items/c.dbr"], family.RecordIds);
        Assert.Equal(["Rare"], family.Rarities);
    }
}
