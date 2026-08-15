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

            INSERT INTO records(id, record_id, class, name_tag, display_name) VALUES
                (1, 'records/items/a.dbr', 'Item', 'tagAlpha', 'Alpha'),
                (2, 'records/items/b.dbr', 'Item', 'tagShared', 'Beta'),
                (3, 'records/items/c.dbr', 'Item', 'tagShared', 'Alpine'),
                (4, 'records/items/percent.dbr', 'Item', '', '100% Blade'),
                (5, 'records/affixes/a.dbr', 'Affix', NULL, 'Balanced'),
                (6, 'records/affixes/b.dbr', 'Affix', NULL, 'Savage'),
                (900, 'records/items/lootaffixes/ascended/a.dbr', 'LootRandomizer', NULL, 'Ascended Power'),
                (901, 'records/skills/itemskillsgdx3/skillmodifiers/ascended/a.dbr', 'SkillModifier', NULL, 'Skill Power');

            INSERT INTO items(record_pk, rarity, item_class, item_level, required_level, is_mi) VALUES
                (1, 'Common', 'Sword', 1, 1, 0),
                (2, 'Rare', 'Mace', 10, 10, 1),
                (3, 'Rare', 'Mace', 20, 20, 0),
                (4, 'Rare', 'Sword', 30, 30, 0);

            INSERT INTO affixes(record_pk, family, kind, rarity, item_level, required_level, jitter_percent) VALUES
                (5, 'standard', 'prefix', 'Rare', 10, 10, 10),
                (6, 'standard', 'suffix', 'Magical', 20, 20, 5),
                (900, 'ascended', NULL, 'Legendary', 94, 94, 0);

            INSERT INTO affix_item_classes(item_class, affix_pk) VALUES
                ('Mace', 5),
                ('Sword', 6);

            INSERT INTO ascended_affix_categories(affix_pk, category, group_name) VALUES
                (900, 'oneHandMelee', 'affix');

            INSERT INTO record_skill_modifiers(owner_pk, modifier_pk, ordinal, skill_pk) VALUES
                (900, 901, 1, NULL);
            """);
        _execute(connection, GdCli.Database.DatabaseSchema.CreateIndexesSql);
        _execute(connection, GdCli.Database.DatabaseSchema.CreateBuildIndexesSql);
    }

    public string Path { get; }

    public void Execute(string sql)
    {
        var connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = Path,
            Pooling = false
        }.ToString();
        using var connection = new SqliteConnection(connectionString);
        connection.Open();
        _execute(connection, sql);
    }

    public void Execute(Action<SqliteConnection, SqliteTransaction> action)
    {
        var connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = Path,
            Pooling = false
        }.ToString();
        using var connection = new SqliteConnection(connectionString);
        connection.Open();
        using var transaction = connection.BeginTransaction();
        action(connection, transaction);
        transaction.Commit();
    }

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
