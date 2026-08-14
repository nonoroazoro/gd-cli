using GdCli.Contracts;
using Microsoft.Data.Sqlite;

namespace GdCli.Database;

internal sealed class QuestDetailLoader
{
    private readonly SqliteConnection _connection;

    public QuestDetailLoader(SqliteConnection connection)
    {
        _connection = connection;
    }

    public void Populate(IReadOnlyList<QuestRecord> quests)
    {
        if (quests.Count == 0)
            return;
        foreach (var chunk in quests.Select(quest => quest.QuestPath).Chunk(300))
            _populateChunk(quests, chunk);
    }

    private void _populateChunk(IReadOnlyList<QuestRecord> quests, string[] paths)
    {
        var selected = quests
            .Where(quest => paths.Contains(quest.QuestPath, StringComparer.OrdinalIgnoreCase))
            .ToDictionary(quest => quest.QuestPath, StringComparer.OrdinalIgnoreCase);
        var nodes = _loadNodes(paths);
        var conditions = _loadOperations(paths, "quest_conditions", true);
        var actions = _loadOperations(paths, "quest_actions", false);
        foreach (var entry in nodes)
        {
            foreach (var node in entry.Value)
            {
                node.Conditions = conditions.GetValueOrDefault(node.NodeId) ?? [];
                node.Actions = actions.GetValueOrDefault(node.NodeId) ?? [];
            }
            selected[entry.Key].Nodes = entry.Value;
        }
        var edges = _loadEdges(paths);
        var entities = _loadEntities(paths);
        _loadLocations(paths, entities.ById);
        var unresolved = _loadUnresolved(paths);
        foreach (var path in paths)
        {
            var quest = selected[path];
            quest.Nodes ??= [];
            quest.Edges = edges.GetValueOrDefault(path) ?? [];
            quest.Entities = entities.ByQuest.GetValueOrDefault(path) ?? [];
            quest.UnresolvedReferences = unresolved.GetValueOrDefault(path) ?? [];
        }
    }

    private Dictionary<string, List<QuestNodeRecord>> _loadNodes(string[] paths)
    {
        using var command = _connection.CreateCommand();
        var parameters = SqliteQuery.AddValues(command, "quest", paths);
        command.CommandText = $"""
            SELECT
                Q.quest_path, N.id, N.parent_pk, N.ordinal, N.kind, N.phase, N.uid, N.link_id,
                N.is_blocker, N.dont_propagate, N.name, N.description,
                N.flags, N.condition_operator, N.origin_path
            FROM quest_nodes N
            JOIN quests Q ON Q.id = N.quest_pk
            WHERE Q.quest_path IN ({parameters})
            ORDER BY Q.quest_path, N.id
            """;
        using var reader = command.ExecuteReader();
        var result = new Dictionary<string, List<QuestNodeRecord>>(StringComparer.OrdinalIgnoreCase);
        while (reader.Read())
        {
            var path = reader.GetString(0);
            if (!result.TryGetValue(path, out var nodes))
            {
                nodes = [];
                result[path] = nodes;
            }
            nodes.Add(new QuestNodeRecord
            {
                NodeId = reader.GetInt64(1),
                ParentNodeId = reader.IsDBNull(2) ? null : reader.GetInt64(2),
                Ordinal = reader.GetInt32(3),
                Kind = reader.GetString(4),
                Phase = reader.GetString(5),
                Uid = reader.IsDBNull(6) ? null : reader.GetInt64(6),
                LinkId = reader.IsDBNull(7) ? null : reader.GetInt32(7),
                IsBlocker = reader.IsDBNull(8) ? null : reader.GetInt32(8) != 0,
                DontPropagate = reader.IsDBNull(9) ? null : reader.GetInt32(9) != 0,
                Name = reader.GetString(10),
                Description = reader.GetString(11),
                Flags = reader.GetInt64(12),
                ConditionOperator = reader.GetString(13),
                Origin = reader.GetString(14)
            });
        }
        return result;
    }

    private Dictionary<long, IReadOnlyList<QuestOperationRecord>> _loadOperations(
        string[] paths,
        string table,
        bool hasComparison)
    {
        using var command = _connection.CreateCommand();
        var parameters = SqliteQuery.AddValues(command, "quest", paths);
        var comparison = hasComparison ? "V.comparison" : "NULL";
        command.CommandText = $"""
            SELECT
                V.node_pk, V.kind, {comparison}, V.quest_path, V.task_uid, V.objective_uid,
                V.record_id, V.token, V.function_name, V.text_value, V.numeric_value,
                V.secondary_numeric_value, V.tertiary_numeric_value, V.boolean_value
            FROM {table} V
            JOIN quest_nodes N ON N.id = V.node_pk
            JOIN quests Q ON Q.id = N.quest_pk
            WHERE Q.quest_path IN ({parameters})
            ORDER BY V.node_pk, V.ordinal
            """;
        using var reader = command.ExecuteReader();
        var mutable = new Dictionary<long, List<QuestOperationRecord>>();
        while (reader.Read())
        {
            var node = reader.GetInt64(0);
            if (!mutable.TryGetValue(node, out var values))
            {
                values = [];
                mutable[node] = values;
            }
            values.Add(new QuestOperationRecord
            {
                Kind = reader.GetString(1),
                Comparison = reader.IsDBNull(2) ? null : reader.GetInt32(2),
                QuestPath = reader.IsDBNull(3) ? null : reader.GetString(3),
                TaskUid = reader.IsDBNull(4) ? null : reader.GetInt64(4),
                ObjectiveUid = reader.IsDBNull(5) ? null : reader.GetInt64(5),
                RecordId = reader.IsDBNull(6) ? null : reader.GetString(6),
                Token = reader.IsDBNull(7) ? null : reader.GetString(7),
                Function = reader.IsDBNull(8) ? null : reader.GetString(8),
                TextValue = reader.IsDBNull(9) ? null : reader.GetString(9),
                NumericValue = reader.IsDBNull(10) ? null : reader.GetDouble(10),
                SecondaryNumericValue = reader.IsDBNull(11) ? null : reader.GetDouble(11),
                TertiaryNumericValue = reader.IsDBNull(12) ? null : reader.GetDouble(12),
                BooleanValue = reader.IsDBNull(13) ? null : reader.GetInt32(13) != 0
            });
        }
        return mutable.ToDictionary(entry => entry.Key, entry => (IReadOnlyList<QuestOperationRecord>)entry.Value);
    }

    private Dictionary<string, IReadOnlyList<QuestEdgeRecord>> _loadEdges(string[] paths)
    {
        using var command = _connection.CreateCommand();
        var parameters = SqliteQuery.AddValues(command, "quest", paths);
        command.CommandText = $"""
            SELECT Q.quest_path, E.source_node_pk, E.target_quest_path, E.target_task_uid, E.kind, E.origin_path
            FROM quest_edges E
            JOIN quests Q ON Q.id = E.quest_pk
            WHERE Q.quest_path IN ({parameters})
            ORDER BY Q.quest_path, E.id
            """;
        using var reader = command.ExecuteReader();
        var mutable = new Dictionary<string, List<QuestEdgeRecord>>(StringComparer.OrdinalIgnoreCase);
        while (reader.Read())
        {
            var path = reader.GetString(0);
            if (!mutable.TryGetValue(path, out var values))
            {
                values = [];
                mutable[path] = values;
            }
            values.Add(new QuestEdgeRecord
            {
                SourceNodeId = reader.GetInt64(1),
                TargetQuestPath = reader.GetString(2),
                TargetTaskUid = reader.IsDBNull(3) ? null : reader.GetInt64(3),
                Kind = reader.GetString(4),
                Origin = reader.GetString(5)
            });
        }
        return mutable.ToDictionary(entry => entry.Key, entry => (IReadOnlyList<QuestEdgeRecord>)entry.Value, StringComparer.OrdinalIgnoreCase);
    }

    private (Dictionary<long, QuestEntityRecord> ById, Dictionary<string, IReadOnlyList<QuestEntityRecord>> ByQuest)
        _loadEntities(string[] paths)
    {
        using var command = _connection.CreateCommand();
        var parameters = SqliteQuery.AddValues(command, "quest", paths);
        command.CommandText = $"""
            SELECT Q.quest_path, E.id, E.node_pk, E.role, R.record_id, R.display_name, E.origin_path
            FROM quest_entities E
            JOIN quests Q ON Q.id = E.quest_pk
            JOIN records R ON R.id = E.record_pk
            WHERE Q.quest_path IN ({parameters})
            ORDER BY Q.quest_path, E.id
            """;
        using var reader = command.ExecuteReader();
        var byId = new Dictionary<long, QuestEntityRecord>();
        var mutable = new Dictionary<string, List<QuestEntityRecord>>(StringComparer.OrdinalIgnoreCase);
        var grouped = new Dictionary<string, QuestEntityRecord>(StringComparer.OrdinalIgnoreCase);
        while (reader.Read())
        {
            var path = reader.GetString(0);
            var id = reader.GetInt64(1);
            var role = reader.GetString(3);
            var recordId = reader.GetString(4);
            var groupKey = $"{path}\0{role}\0{recordId}";
            if (!grouped.TryGetValue(groupKey, out var entity))
            {
                entity = new QuestEntityRecord
                {
                    Role = role,
                    RecordId = recordId,
                    Name = reader.GetString(5)
                };
                grouped[groupKey] = entity;
                if (!mutable.TryGetValue(path, out var values))
                {
                    values = [];
                    mutable[path] = values;
                }
                values.Add(entity);
            }
            if (!reader.IsDBNull(2))
            {
                var nodeId = reader.GetInt64(2);
                if (!entity.NodeIds.Contains(nodeId))
                    entity.NodeIds.Add(nodeId);
            }
            var origin = reader.GetString(6);
            if (!entity.Origins.Contains(origin, StringComparer.OrdinalIgnoreCase))
                entity.Origins.Add(origin);
            byId[id] = entity;
        }
        return (
            byId,
            mutable.ToDictionary(entry => entry.Key, entry => (IReadOnlyList<QuestEntityRecord>)entry.Value, StringComparer.OrdinalIgnoreCase));
    }

    private void _loadLocations(string[] paths, Dictionary<long, QuestEntityRecord> entities)
    {
        using var command = _connection.CreateCommand();
        var parameters = SqliteQuery.AddValues(command, "quest", paths);
        command.CommandText = $"""
            SELECT
                E.id, 'direct', L.source_name, L.level_path, L.rift_gate_record_id,
                R.record_id, P.world_x, P.world_y, P.world_z
            FROM quest_entities E
            JOIN quests Q ON Q.id = E.quest_pk
            JOIN placements P ON P.record_pk = E.record_pk
            JOIN levels L ON L.id = P.level_pk
            JOIN records R ON R.id = P.record_pk
            WHERE Q.quest_path IN ({parameters})
            UNION ALL
            SELECT
                E.id, 'scriptState', L.source_name, L.level_path, L.rift_gate_record_id,
                PR.record_id, P.world_x, P.world_y, P.world_z
            FROM quest_entities E
            JOIN quests Q ON Q.id = E.quest_pk
            JOIN entity_aliases A ON A.alias_pk = E.record_pk
            JOIN placements P ON P.record_pk = A.placed_pk
            JOIN levels L ON L.id = P.level_pk
            JOIN records PR ON PR.id = P.record_pk
            WHERE Q.quest_path IN ({parameters})
            ORDER BY 1, 3, 4, 6, 7, 8, 9
            """;
        using var reader = command.ExecuteReader();
        var locations = new Dictionary<long, List<QuestLocation>>();
        while (reader.Read())
        {
            var entityId = reader.GetInt64(0);
            if (!locations.TryGetValue(entityId, out var values))
            {
                values = [];
                locations[entityId] = values;
            }
            values.Add(new QuestLocation
            {
                Resolution = reader.GetString(1),
                Source = reader.GetString(2),
                Level = reader.GetString(3),
                RiftGateRecordId = reader.GetString(4),
                PlacedRecordId = reader.GetString(5),
                X = reader.GetDouble(6),
                Y = reader.GetDouble(7),
                Z = reader.GetDouble(8)
            });
        }
        foreach (var entry in locations)
        {
            if (entities.TryGetValue(entry.Key, out var entity))
            {
                entity.Locations = entity.Locations.Concat(entry.Value)
                    .DistinctBy(location => (
                        location.Resolution,
                        location.Source,
                        location.Level,
                        location.PlacedRecordId,
                        location.X,
                        location.Y,
                        location.Z))
                    .ToArray();
            }
        }
    }

    private Dictionary<string, IReadOnlyList<QuestUnresolvedReference>> _loadUnresolved(string[] paths)
    {
        using var command = _connection.CreateCommand();
        var parameters = SqliteQuery.AddValues(command, "quest", paths);
        command.CommandText = $"""
            SELECT Q.quest_path, U.node_pk, U.kind, U.value, U.origin_path
            FROM quest_unresolved_references U
            JOIN quests Q ON Q.id = U.quest_pk
            WHERE Q.quest_path IN ({parameters})
            ORDER BY Q.quest_path, U.id
            """;
        using var reader = command.ExecuteReader();
        var mutable = new Dictionary<string, List<QuestUnresolvedReference>>(StringComparer.OrdinalIgnoreCase);
        while (reader.Read())
        {
            var path = reader.GetString(0);
            if (!mutable.TryGetValue(path, out var values))
            {
                values = [];
                mutable[path] = values;
            }
            values.Add(new QuestUnresolvedReference
            {
                NodeId = reader.IsDBNull(1) ? null : reader.GetInt64(1),
                Kind = reader.GetString(2),
                Value = reader.GetString(3),
                Origin = reader.GetString(4)
            });
        }
        return mutable.ToDictionary(entry => entry.Key, entry => (IReadOnlyList<QuestUnresolvedReference>)entry.Value, StringComparer.OrdinalIgnoreCase);
    }

}
