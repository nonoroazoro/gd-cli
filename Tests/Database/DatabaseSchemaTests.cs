using Microsoft.Data.Sqlite;

namespace GdCli.Tests.Database;

public sealed class DatabaseSchemaTests
{
    [Fact]
    public void AcquisitionSourcesRejectDuplicateRandomDrop()
    {
        using var fixture = new TestDatabase();
        fixture.Execute("""
            INSERT INTO acquisition_sources(item_pk, kind, source_pk)
            VALUES (1, 'randomDrop', NULL);
            """);

        Assert.Throws<SqliteException>(() => fixture.Execute("""
            INSERT INTO acquisition_sources(item_pk, kind, source_pk)
            VALUES (1, 'randomDrop', NULL);
            """));
    }
}
