using GdCli.GameData.Conversations;
using GdCli.GameData.Scriptables;

namespace GdCli.Database;

internal static class ConversationQuestImporter
{
    public static void Import(
        QuestDatabaseWriter writer,
        IEnumerable<ConversationDefinition> conversations,
        IReadOnlyDictionary<string, long> questPks,
        IReadOnlyDictionary<string, List<long>> resourceActors)
    {
        foreach (var conversation in conversations)
        {
            resourceActors.TryGetValue(conversation.Path, out var actors);
            foreach (var questPath in _referencedQuests(conversation.Root, questPks))
            {
                var questPk = questPks[questPath];
                var relevantSteps = new HashSet<int>();
                _collectRelevantSteps(conversation.Root, questPath, relevantSteps);
                if (actors == null)
                {
                    writer.InsertUnresolved(
                        questPk,
                        null,
                        "conversationActor",
                        conversation.Path,
                        conversation.Path);
                }
                _insertTree(
                    writer,
                    conversation,
                    conversation.Root,
                    questPath,
                    questPk,
                    null,
                    relevantSteps,
                    actors);
            }
        }
    }

    private static IEnumerable<string> _referencedQuests(
        ConversationStep root,
        IReadOnlyDictionary<string, long> questPks)
    {
        return ConversationReader.Traverse(root)
            .SelectMany(step => step.Conditions.Values.Concat(step.Actions))
            .Where(value => value.QuestPath != null && questPks.ContainsKey(value.QuestPath))
            .Select(value => value.QuestPath ?? string.Empty)
            .Distinct(StringComparer.OrdinalIgnoreCase);
    }

    private static bool _collectRelevantSteps(
        ConversationStep step,
        string questPath,
        HashSet<int> relevantSteps)
    {
        var relevant = _referencesQuest(step, questPath);
        foreach (var child in step.Children)
            relevant |= _collectRelevantSteps(child, questPath, relevantSteps);
        if (relevant)
            relevantSteps.Add(step.Ordinal);
        return relevant;
    }

    private static bool _referencesQuest(ConversationStep step, string questPath)
    {
        return step.Conditions.Values.Concat(step.Actions).Any(value =>
            value.QuestPath != null &&
            value.QuestPath.Equals(questPath, StringComparison.OrdinalIgnoreCase));
    }

    private static void _insertTree(
        QuestDatabaseWriter writer,
        ConversationDefinition conversation,
        ConversationStep step,
        string questPath,
        long questPk,
        long? parentPk,
        HashSet<int> relevantSteps,
        IReadOnlyList<long>? actors)
    {
        if (!relevantSteps.Contains(step.Ordinal))
            return;
        var nodePk = writer.InsertNode(
            questPk,
            parentPk,
            step.Ordinal,
            "conversation",
            step.Type,
            null,
            step.LinkId,
            null,
            null,
            string.Empty,
            string.Empty,
            step.Flags,
            step.Conditions.Operator,
            conversation.Path);
        writer.InsertOperations(
            questPath,
            questPk,
            nodePk,
            step.Conditions,
            step.Actions);
        if (actors != null && _referencesQuest(step, questPath))
        {
            var role = _role(step.Actions, questPath);
            foreach (var actor in actors)
                writer.InsertEntity(questPk, nodePk, actor, role, conversation.Path);
        }
        foreach (var child in step.Children)
        {
            _insertTree(
                writer,
                conversation,
                child,
                questPath,
                questPk,
                nodePk,
                relevantSteps,
                actors);
        }
    }

    private static string _role(IReadOnlyList<ScriptableValue> actions, string questPath)
    {
        if (_hasQuestAction(actions, questPath, "CompleteQuest", "CompleteQuestTask"))
            return "turnIn";
        if (_hasQuestAction(actions, questPath, "BeginQuest", "BeginQuestTask"))
            return "offer";
        return "participant";
    }

    private static bool _hasQuestAction(
        IReadOnlyList<ScriptableValue> actions,
        string questPath,
        params string[] kinds)
    {
        return actions.Any(action =>
            action.QuestPath != null &&
            action.QuestPath.Equals(questPath, StringComparison.OrdinalIgnoreCase) &&
            kinds.Contains(action.Kind, StringComparer.Ordinal));
    }
}
