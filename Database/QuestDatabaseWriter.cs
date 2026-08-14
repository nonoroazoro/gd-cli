using System.Globalization;
using GdCli.GameData.Quests;
using GdCli.GameData.Scriptables;
using Microsoft.Data.Sqlite;

namespace GdCli.Database;

internal sealed class QuestDatabaseWriter
{
    private readonly SqliteConnection _connection;
    private readonly SqliteTransaction _transaction;
    private readonly IReadOnlyDictionary<string, long> _records;

    public QuestDatabaseWriter(
        SqliteConnection connection,
        SqliteTransaction transaction,
        IReadOnlyDictionary<string, long> records)
    {
        _connection = connection;
        _transaction = transaction;
        _records = records;
    }

    public long InsertQuest(QuestDefinition quest)
    {
        using var command = _command("""
            INSERT INTO quests(quest_path, source_name, uid, flags, region, name)
            VALUES (@path, @source, @uid, @flags, @region, @name)
            RETURNING id
            """);
        command.Parameters.AddWithValue("@path", quest.Path);
        command.Parameters.AddWithValue("@source", quest.Source);
        command.Parameters.AddWithValue("@uid", (long)quest.Uid);
        command.Parameters.AddWithValue("@flags", (long)quest.Flags);
        command.Parameters.AddWithValue("@region", quest.Region);
        command.Parameters.AddWithValue("@name", quest.Name);
        return Convert.ToInt64(command.ExecuteScalar(), CultureInfo.InvariantCulture);
    }

    public long InsertNode(
        long questPk,
        long? parentPk,
        int ordinal,
        string kind,
        string phase,
        uint? uid,
        int? linkId,
        bool? isBlocker,
        bool? dontPropagate,
        string name,
        string description,
        uint flags,
        string conditionOperator,
        string originPath)
    {
        using var command = _command("""
            INSERT INTO quest_nodes(
                quest_pk, parent_pk, ordinal, kind, phase, uid, link_id,
                is_blocker, dont_propagate, name, description,
                flags, condition_operator, origin_path)
            VALUES (
                @quest, @parent, @ordinal, @kind, @phase, @uid, @link,
                @blocker, @propagate, @name, @description,
                @flags, @operator, @origin)
            RETURNING id
            """);
        command.Parameters.AddWithValue("@quest", questPk);
        command.Parameters.AddWithValue("@parent", _dbValue(parentPk));
        command.Parameters.AddWithValue("@ordinal", ordinal);
        command.Parameters.AddWithValue("@kind", kind);
        command.Parameters.AddWithValue("@phase", phase);
        command.Parameters.AddWithValue("@uid", _dbValue(uid));
        command.Parameters.AddWithValue("@link", _dbValue(linkId));
        command.Parameters.AddWithValue("@blocker", _dbBoolean(isBlocker));
        command.Parameters.AddWithValue("@propagate", _dbBoolean(dontPropagate));
        command.Parameters.AddWithValue("@name", name);
        command.Parameters.AddWithValue("@description", description);
        command.Parameters.AddWithValue("@flags", (long)flags);
        command.Parameters.AddWithValue("@operator", conditionOperator);
        command.Parameters.AddWithValue("@origin", originPath);
        return Convert.ToInt64(command.ExecuteScalar(), CultureInfo.InvariantCulture);
    }

    public void InsertOperations(
        string questPath,
        long questPk,
        long nodePk,
        ScriptableGroup conditions,
        IReadOnlyList<ScriptableValue> actions)
    {
        _insertValues("quest_conditions", nodePk, conditions.Values, true);
        _insertValues("quest_actions", nodePk, actions, false);
        foreach (var action in actions)
        {
            var kind = action.Kind switch
            {
                "BeginQuest" or "BeginQuestTask" => "begin",
                "CompleteQuestTask" => "completeTask",
                "CompleteQuest" => "completeQuest",
                _ => null
            };
            if (kind != null && action.QuestPath != null)
                InsertEdge(questPk, nodePk, action.QuestPath, action.TaskUid, kind, questPath);
        }
        foreach (var condition in conditions.Values)
            _insertReferencedEntities(questPk, nodePk, condition, questPath);
    }

    public void InsertEdge(
        long questPk,
        long nodePk,
        string questPath,
        uint? taskUid,
        string kind,
        string originPath)
    {
        using var command = _command("""
            INSERT INTO quest_edges(
                quest_pk, source_node_pk, target_quest_path, target_task_uid, kind, origin_path)
            VALUES (@quest, @source, @path, @task, @kind, @origin)
            """);
        command.Parameters.AddWithValue("@quest", questPk);
        command.Parameters.AddWithValue("@source", nodePk);
        command.Parameters.AddWithValue("@path", questPath);
        command.Parameters.AddWithValue("@task", _dbValue(taskUid));
        command.Parameters.AddWithValue("@kind", kind);
        command.Parameters.AddWithValue("@origin", originPath);
        command.ExecuteNonQuery();
    }

    public void InsertEntity(
        long questPk,
        long? nodePk,
        long recordPk,
        string role,
        string originPath)
    {
        using var command = _command("""
            INSERT OR IGNORE INTO quest_entities(quest_pk, node_pk, record_pk, role, origin_path)
            VALUES (@quest, @node, @record, @role, @origin)
            """);
        command.Parameters.AddWithValue("@quest", questPk);
        command.Parameters.AddWithValue("@node", _dbValue(nodePk));
        command.Parameters.AddWithValue("@record", recordPk);
        command.Parameters.AddWithValue("@role", role);
        command.Parameters.AddWithValue("@origin", originPath);
        command.ExecuteNonQuery();
    }

    public void InsertUnresolved(
        long questPk,
        long? nodePk,
        string kind,
        string value,
        string originPath)
    {
        using var command = _command("""
            INSERT OR IGNORE INTO quest_unresolved_references(
                quest_pk, node_pk, kind, value, origin_path)
            VALUES (@quest, @node, @kind, @value, @origin)
            """);
        command.Parameters.AddWithValue("@quest", questPk);
        command.Parameters.AddWithValue("@node", _dbValue(nodePk));
        command.Parameters.AddWithValue("@kind", kind);
        command.Parameters.AddWithValue("@value", value);
        command.Parameters.AddWithValue("@origin", originPath);
        command.ExecuteNonQuery();
    }

    public void InsertAlias(long aliasPk, long placedPk, string originPath)
    {
        using var command = _command("""
            INSERT OR IGNORE INTO entity_aliases(alias_pk, placed_pk, origin_path)
            VALUES (@alias, @placed, @origin)
            """);
        command.Parameters.AddWithValue("@alias", aliasPk);
        command.Parameters.AddWithValue("@placed", placedPk);
        command.Parameters.AddWithValue("@origin", originPath);
        command.ExecuteNonQuery();
    }

    public bool TryGetRecord(string recordId, out long recordPk)
    {
        return _records.TryGetValue(recordId, out recordPk);
    }

    private void _insertValues(
        string table,
        long nodePk,
        IReadOnlyList<ScriptableValue> values,
        bool includeComparison)
    {
        var comparisonColumn = includeComparison ? "comparison," : string.Empty;
        var comparisonValue = includeComparison ? "@comparison," : string.Empty;
        using var command = _command($"""
            INSERT INTO {table}(
                node_pk, ordinal, kind, {comparisonColumn} quest_path, task_uid, objective_uid,
                record_id, token, function_name, text_value, numeric_value,
                secondary_numeric_value, tertiary_numeric_value, boolean_value)
            VALUES (
                @node, @ordinal, @kind, {comparisonValue} @questPath, @task, @objective,
                @record, @token, @function, @text, @numeric,
                @secondary, @tertiary, @boolean)
            """);
        for (var ordinal = 0; ordinal < values.Count; ordinal++)
        {
            var value = values[ordinal];
            command.Parameters.Clear();
            command.Parameters.AddWithValue("@node", nodePk);
            command.Parameters.AddWithValue("@ordinal", ordinal);
            command.Parameters.AddWithValue("@kind", value.Kind);
            if (includeComparison)
                command.Parameters.AddWithValue("@comparison", _dbValue(value.Comparison));
            command.Parameters.AddWithValue("@questPath", _dbValue(value.QuestPath));
            command.Parameters.AddWithValue("@task", _dbValue(value.TaskUid));
            command.Parameters.AddWithValue("@objective", _dbValue(value.ObjectiveUid));
            command.Parameters.AddWithValue("@record", _dbValue(value.RecordId));
            command.Parameters.AddWithValue("@token", _dbValue(value.Token));
            command.Parameters.AddWithValue("@function", _dbValue(value.Function));
            command.Parameters.AddWithValue("@text", _dbValue(value.TextValue));
            command.Parameters.AddWithValue("@numeric", _dbValue(value.NumericValue));
            command.Parameters.AddWithValue("@secondary", _dbValue(value.SecondaryNumericValue));
            command.Parameters.AddWithValue("@tertiary", _dbValue(value.TertiaryNumericValue));
            command.Parameters.AddWithValue("@boolean", _dbBoolean(value.BooleanValue));
            command.ExecuteNonQuery();
        }
    }

    private void _insertReferencedEntities(
        long questPk,
        long nodePk,
        ScriptableValue value,
        string originPath)
    {
        var recordIds = value.RecordId == null ? value.RecordIds : value.RecordIds.Prepend(value.RecordId);
        foreach (var recordId in recordIds.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (_records.TryGetValue(recordId, out var recordPk))
                InsertEntity(questPk, nodePk, recordPk, "target", originPath);
            else
                InsertUnresolved(questPk, nodePk, "record", recordId, originPath);
        }
    }

    private SqliteCommand _command(string sql)
    {
        var command = _connection.CreateCommand();
        command.Transaction = _transaction;
        command.CommandText = sql;
        return command;
    }

    private static object _dbValue(object? value)
    {
        return value switch
        {
            null => DBNull.Value,
            uint unsigned => (long)unsigned,
            _ => value
        };
    }

    private static object _dbBoolean(bool? value)
    {
        return value switch
        {
            true => 1,
            false => 0,
            null => DBNull.Value
        };
    }
}
