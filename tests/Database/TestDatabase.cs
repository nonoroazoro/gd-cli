using Microsoft.Data.Sqlite;

namespace GdCli.Tests.Database;

internal sealed class TestDatabase : IDisposable
{
    private readonly string _directory;

    public TestDatabase()
    {
        _directory = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "gd-cli-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_directory);
        Path = System.IO.Path.Combine(_directory, "gd-cli.db");

        var connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = Path,
            Pooling = false
        }.ToString();
        using var connection = new SqliteConnection(connectionString);
        connection.Open();
        _execute(connection, GdCli.Database.DatabaseSchema.CreateSql);
        _execute(connection, """
            INSERT INTO metadata(key, value) VALUES
                ('gameLanguage', 'EN'),
                ('gameDirectory', 'test');

            INSERT INTO records(id, record_id, source_name, class, display_name) VALUES
                (1, 'records/items/a.dbr', 'base', 'Item', 'Alpha'),
                (2, 'records/items/b.dbr', 'base', 'Item', 'Beta'),
                (3, 'records/items/c.dbr', 'base', 'Item', 'Alpine'),
                (4, 'records/items/percent.dbr', 'base', 'Item', '100% Blade'),
                (5, 'records/affixes/a.dbr', 'base', 'Affix', 'Balanced'),
                (6, 'records/affixes/b.dbr', 'base', 'Affix', 'Savage');

            INSERT INTO items(record_pk, name, rarity, item_class, item_level, required_level, is_mi) VALUES
                (1, 'Alpha', 'Common', 'Sword', 1, 1, 0),
                (2, 'Beta', 'Rare', 'Mace', 10, 10, 1),
                (3, 'Alpine', 'Rare', 'Mace', 20, 20, 0),
                (4, '100% Blade', 'Rare', 'Sword', 30, 30, 0);

            INSERT INTO affixes(record_pk, name, kind, rarity, item_level, required_level, jitter_percent) VALUES
                (5, 'Balanced', 'prefix', 'Rare', 10, 10, 10),
                (6, 'Savage', 'suffix', 'Magical', 20, 20, 5);
            """);
        _execute(connection, GdCli.Database.DatabaseSchema.CreateIndexesSql);
    }

    public string Path { get; }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
            Directory.Delete(_directory, true);
    }

    private static void _execute(SqliteConnection connection, string sql)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }
}
