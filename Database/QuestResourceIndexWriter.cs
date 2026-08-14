using Microsoft.Data.Sqlite;

namespace GdCli.Database;

internal sealed class QuestResourceIndexWriter : IDisposable
{
    private readonly SqliteCommand _deleteResourceActors;
    private readonly SqliteCommand _deleteScriptBindings;
    private readonly SqliteCommand _insertResourceActor;
    private readonly SqliteCommand _insertScriptBinding;

    public QuestResourceIndexWriter(SqliteConnection connection, SqliteTransaction transaction)
    {
        _execute(connection, transaction, """
            CREATE TEMP TABLE raw_resource_actors (
                resource_path TEXT NOT NULL COLLATE NOCASE,
                record_pk INTEGER NOT NULL,
                PRIMARY KEY (resource_path, record_pk)
            ) WITHOUT ROWID;

            CREATE TEMP TABLE raw_script_bindings (
                function_name TEXT NOT NULL COLLATE NOCASE,
                record_pk INTEGER NOT NULL,
                PRIMARY KEY (function_name, record_pk)
            ) WITHOUT ROWID;
            """);
        _deleteResourceActors = _command(
            connection,
            transaction,
            "DELETE FROM raw_resource_actors WHERE record_pk = @record");
        _deleteResourceActors.Parameters.Add("@record", SqliteType.Integer);
        _deleteScriptBindings = _command(
            connection,
            transaction,
            "DELETE FROM raw_script_bindings WHERE record_pk = @record");
        _deleteScriptBindings.Parameters.Add("@record", SqliteType.Integer);
        _insertResourceActor = _command(connection, transaction, """
            INSERT OR IGNORE INTO raw_resource_actors(resource_path, record_pk)
            VALUES (@path, @record)
            """);
        _insertResourceActor.Parameters.Add("@path", SqliteType.Text);
        _insertResourceActor.Parameters.Add("@record", SqliteType.Integer);
        _insertScriptBinding = _command(connection, transaction, """
            INSERT OR IGNORE INTO raw_script_bindings(function_name, record_pk)
            VALUES (@function, @record)
            """);
        _insertScriptBinding.Parameters.Add("@function", SqliteType.Text);
        _insertScriptBinding.Parameters.Add("@record", SqliteType.Integer);
    }

    public void Reset(long recordPk)
    {
        _deleteResourceActors.Parameters["@record"].Value = recordPk;
        _deleteResourceActors.ExecuteNonQuery();
        _deleteScriptBindings.Parameters["@record"].Value = recordPk;
        _deleteScriptBindings.ExecuteNonQuery();
    }

    public void Capture(long recordPk, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return;
        if (value.EndsWith(".cnv", StringComparison.OrdinalIgnoreCase) ||
            value.EndsWith(".qst", StringComparison.OrdinalIgnoreCase))
        {
            _insertResourceActor.Parameters["@path"].Value = value.Replace('\\', '/').ToLowerInvariant();
            _insertResourceActor.Parameters["@record"].Value = recordPk;
            _insertResourceActor.ExecuteNonQuery();
        }
        if (!_isLuaFunction(value))
            return;
        _insertScriptBinding.Parameters["@function"].Value = value;
        _insertScriptBinding.Parameters["@record"].Value = recordPk;
        _insertScriptBinding.ExecuteNonQuery();
    }

    public void Dispose()
    {
        _deleteResourceActors.Dispose();
        _deleteScriptBindings.Dispose();
        _insertResourceActor.Dispose();
        _insertScriptBinding.Dispose();
    }

    private static bool _isLuaFunction(string value)
    {
        if (value.Contains('/') || value.Contains('\\') || value.Any(char.IsWhiteSpace))
            return false;
        var segments = value.Split('.');
        return segments.Length >= 2 && segments.All(segment =>
            segment.Length > 0 &&
            (char.IsLetter(segment[0]) || segment[0] == '_') &&
            segment.Skip(1).All(character => char.IsLetterOrDigit(character) || character == '_'));
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

    private static void _execute(SqliteConnection connection, SqliteTransaction transaction, string sql)
    {
        using var command = _command(connection, transaction, sql);
        command.ExecuteNonQuery();
    }
}
