using GdCli.GameData.Quests;

namespace GdCli.Database;

internal static class QuestDefinitionImporter
{
    public static Dictionary<string, long> Import(
        QuestDatabaseWriter writer,
        IEnumerable<QuestDefinition> quests)
    {
        var ordered = quests.OrderBy(quest => quest.Path, StringComparer.OrdinalIgnoreCase).ToArray();
        var questPks = ordered.ToDictionary(
            quest => quest.Path,
            writer.InsertQuest,
            StringComparer.OrdinalIgnoreCase);
        foreach (var quest in ordered)
            _insertNodes(writer, quest, questPks[quest.Path]);
        return questPks;
    }

    private static void _insertNodes(QuestDatabaseWriter writer, QuestDefinition quest, long questPk)
    {
        foreach (var task in quest.Tasks)
        {
            var taskPk = writer.InsertNode(
                questPk,
                null,
                task.Ordinal,
                "task",
                "task",
                task.Uid,
                null,
                task.IsBlocker,
                task.DontPropagate,
                task.Name,
                task.Description,
                task.Flags,
                "and",
                quest.Path);
            foreach (var entry in task.OnAccept)
                _insertEvent(writer, quest, questPk, taskPk, entry);
            foreach (var objective in task.Objectives)
            {
                var nodePk = writer.InsertNode(
                    questPk,
                    taskPk,
                    objective.Ordinal,
                    "objective",
                    "objective",
                    objective.Uid,
                    null,
                    null,
                    null,
                    objective.Name,
                    string.Empty,
                    objective.Flags,
                    objective.Conditions.Operator,
                    quest.Path);
                writer.InsertOperations(
                    quest.Path,
                    questPk,
                    nodePk,
                    objective.Conditions,
                    objective.Actions);
            }
            foreach (var entry in task.OnComplete)
                _insertEvent(writer, quest, questPk, taskPk, entry);
        }
    }

    private static void _insertEvent(
        QuestDatabaseWriter writer,
        QuestDefinition quest,
        long questPk,
        long taskPk,
        QuestEvent entry)
    {
        var nodePk = writer.InsertNode(
            questPk,
            taskPk,
            entry.Ordinal,
            "event",
            entry.Phase,
            null,
            null,
            null,
            null,
            entry.Name,
            string.Empty,
            entry.Flags,
            entry.Conditions.Operator,
            quest.Path);
        writer.InsertOperations(
            quest.Path,
            questPk,
            nodePk,
            entry.Conditions,
            entry.Actions);
    }
}
