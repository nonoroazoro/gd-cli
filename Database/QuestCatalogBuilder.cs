using GdCli.GameData;
using GdCli.GameData.Quests;
using Microsoft.Data.Sqlite;

namespace GdCli.Database;

internal static class QuestCatalogBuilder
{
    public static void Build(SqliteConnection connection, SqliteTransaction transaction, GameInstall install)
    {
        var sourceCatalog = QuestSourceLoader.Load(install);
        var records = _loadLookup(connection, transaction, "SELECT record_id, id FROM records");
        var resourceActors = _loadGroupedLookup(
            connection,
            transaction,
            "SELECT resource_path, record_pk FROM raw_resource_actors ORDER BY resource_path, record_pk",
            _normalizeResourcePath);
        var scriptBindings = _loadGroupedLookup(
            connection,
            transaction,
            "SELECT function_name, record_pk FROM raw_script_bindings ORDER BY function_name, record_pk");

        var writer = new QuestDatabaseWriter(connection, transaction, records);
        var questPks = QuestDefinitionImporter.Import(writer, sourceCatalog.Quests.Values);
        _importDirectActors(writer, questPks, resourceActors, records);
        ConversationQuestImporter.Import(
            writer,
            sourceCatalog.Conversations.Values,
            questPks,
            resourceActors);
        LuaQuestImporter.Import(
            writer,
            sourceCatalog.LuaFunctions.Values,
            questPks,
            sourceCatalog.Quests.Values.ToDictionary(quest => quest.Uid, quest => quest.Path),
            scriptBindings);

        _execute(connection, transaction, "DROP TABLE raw_resource_actors; DROP TABLE raw_script_bindings;");
    }

    private static Dictionary<string, long> _loadLookup(
        SqliteConnection connection,
        SqliteTransaction transaction,
        string sql)
    {
        using var command = _command(connection, transaction, sql);
        using var reader = command.ExecuteReader();
        var result = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
        while (reader.Read())
            result[reader.GetString(0)] = reader.GetInt64(1);
        return result;
    }

    private static Dictionary<string, List<long>> _loadGroupedLookup(
        SqliteConnection connection,
        SqliteTransaction transaction,
        string sql,
        Func<string, string>? normalizeKey = null)
    {
        using var command = _command(connection, transaction, sql);
        using var reader = command.ExecuteReader();
        var result = new Dictionary<string, List<long>>(StringComparer.OrdinalIgnoreCase);
        while (reader.Read())
        {
            var key = normalizeKey?.Invoke(reader.GetString(0)) ?? reader.GetString(0);
            if (!result.TryGetValue(key, out var values))
            {
                values = [];
                result[key] = values;
            }
            values.Add(reader.GetInt64(1));
        }
        return result;
    }

    private static void _importDirectActors(
        QuestDatabaseWriter writer,
        Dictionary<string, long> questPks,
        Dictionary<string, List<long>> resourceActors,
        Dictionary<string, long> records)
    {
        var recordIds = records.ToDictionary(entry => entry.Value, entry => entry.Key);
        foreach (var quest in questPks)
        {
            if (!resourceActors.TryGetValue(quest.Key, out var actors))
                continue;
            foreach (var actor in actors)
            {
                var recordId = recordIds.GetValueOrDefault(actor) ?? string.Empty;
                var role = recordId.Contains("/creatures/", StringComparison.OrdinalIgnoreCase)
                    ? "participant"
                    : "trigger";
                writer.InsertEntity(quest.Value, null, actor, role, quest.Key);
            }
        }
    }

    private static string _normalizeResourcePath(string path)
    {
        var normalized = path.Replace('\\', '/').ToLowerInvariant();
        if (normalized.EndsWith(".qst", StringComparison.OrdinalIgnoreCase) &&
            !normalized.StartsWith("quests/", StringComparison.Ordinal))
            return $"quests/{normalized}";
        if (normalized.EndsWith(".cnv", StringComparison.OrdinalIgnoreCase) &&
            !normalized.StartsWith("conversations/", StringComparison.Ordinal))
            return $"conversations/{normalized}";
        return normalized;
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
