using GdCli.Database;

namespace GdCli.Tests.Database;

public sealed class AcquisitionRepositoryTests
{
    [Fact]
    public void LoadMiSourcesReturnsSourcesOnlyForMiRecords()
    {
        using var fixture = new TestDatabase();
        fixture.Execute("""
            INSERT INTO records(id, record_id, source_name, class, display_name)
            VALUES (7, 'records/creatures/monster.dbr', 'base', 'Monster', 'Monster');
            INSERT INTO acquisition_sources(item_pk, kind, source_pk) VALUES
                (2, 'specificMonster', 7),
                (3, 'specificMonster', 7);
            """);
        using var database = new CliDatabase(fixture.Path);

        var result = database.Acquisitions.LoadMiSources(
            ["records/items/b.dbr", "records/items/c.dbr"]);

        Assert.Equal(
            "records/creatures/monster.dbr",
            Assert.Single(result["records/items/b.dbr"]).RecordId);
        Assert.False(result.ContainsKey("records/items/c.dbr"));
    }
}
