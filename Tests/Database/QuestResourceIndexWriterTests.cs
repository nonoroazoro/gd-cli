using GdCli.Database;
using Microsoft.Data.Sqlite;

namespace GdCli.Tests.Database;

public sealed class QuestResourceIndexWriterTests
{
    [Fact]
    public void ResetRemovesOverriddenRecordBindings()
    {
        using var database = new TestDatabase();
        database.Execute((connection, transaction) =>
        {
            using var writer = new QuestResourceIndexWriter(connection, transaction);
            writer.Capture(1, "Quests/Test/Old.qst");
            writer.Capture(1, "Quest.oldOnAddToWorld");
            Assert.Equal(1, _scalar(connection, transaction, "SELECT COUNT(*) FROM raw_resource_actors"));
            Assert.Equal(1, _scalar(connection, transaction, "SELECT COUNT(*) FROM raw_script_bindings"));

            writer.Reset(1);
            writer.Capture(1, "Conversations/Test/New.cnv");

            Assert.Equal(
                "conversations/test/new.cnv",
                _text(connection, transaction, "SELECT resource_path FROM raw_resource_actors"));
            Assert.Equal(0, _scalar(connection, transaction, "SELECT COUNT(*) FROM raw_script_bindings"));
        });
    }

    private static long _scalar(
        SqliteConnection connection,
        SqliteTransaction transaction,
        string sql)
    {
        using var command = _command(connection, transaction, sql);
        return Convert.ToInt64(command.ExecuteScalar(), System.Globalization.CultureInfo.InvariantCulture);
    }

    private static string _text(
        SqliteConnection connection,
        SqliteTransaction transaction,
        string sql)
    {
        using var command = _command(connection, transaction, sql);
        return Convert.ToString(command.ExecuteScalar(), System.Globalization.CultureInfo.InvariantCulture)
            ?? string.Empty;
    }

    private static SqliteCommand _command(
        SqliteConnection connection,
        SqliteTransaction transaction,
        string sql)
    {
        var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        return command;
    }
}
