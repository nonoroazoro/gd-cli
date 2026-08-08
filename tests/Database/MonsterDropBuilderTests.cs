using GdCli.Database;
using Microsoft.Data.Sqlite;

namespace GdCli.Tests.Database;

public sealed class MonsterDropBuilderTests
{
    [Fact]
    public void BuildTraversesNestedLootTablesAndCycles()
    {
        using var fixture = new TestDatabase();
        fixture.Execute("""
            UPDATE items SET is_mi = 0;
            UPDATE records
            SET record_id = 'records/items/gearweapons/test.dbr', class = 'WeaponMelee_Mace'
            WHERE id = 2;
            INSERT INTO field_names(id, name) VALUES
                (1, 'lootName1'),
                (2, 'lootName2'),
                (3, 'lootHeadItem1');
            INSERT INTO records(id, record_id, source_name, class, template, display_name) VALUES
                (7, 'records/items/loottables/a.dbr', 'base', 'LootTable', '', ''),
                (8, 'records/items/loottables/b.dbr', 'base', 'LootTable', '', ''),
                (9, 'records/creatures/monster.dbr', 'base', 'Monster', 'database/templates/monster.tpl', 'Monster');
            INSERT INTO record_references(source_pk, field_pk, ordinal, target_pk) VALUES
                (7, 1, 0, 2),
                (8, 1, 0, 7),
                (7, 2, 0, 8),
                (9, 3, 0, 8);
            """);

        fixture.Execute(MonsterDropBuilder.Build);

        using var connection = _openReadOnly(fixture.Path);
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT I.is_mi, M.record_id
            FROM items I
            JOIN monster_drops D ON D.item_pk = I.record_pk
            JOIN records M ON M.id = D.monster_pk
            WHERE I.record_pk = 2
            """;
        using var reader = command.ExecuteReader();
        Assert.True(reader.Read());
        Assert.True(reader.GetBoolean(0));
        Assert.Equal("records/creatures/monster.dbr", reader.GetString(1));
        Assert.False(reader.Read());
        reader.Close();
        command.CommandText = "SELECT COUNT(*) FROM record_references";
        Assert.Equal(4L, command.ExecuteScalar());
    }

    private static SqliteConnection _openReadOnly(string path)
    {
        var connection = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = path,
            Mode = SqliteOpenMode.ReadOnly,
            Pooling = false
        }.ToString());
        connection.Open();
        return connection;
    }
}
