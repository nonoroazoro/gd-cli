using GdCli.Database;
using GdCli.GameData.Conversations;
using GdCli.GameData.Quests;
using GdCli.GameData.Scriptables;

namespace GdCli.Tests.Database;

public sealed class ConversationQuestImporterTests
{
    [Fact]
    public void ImportKeepsRelevantAncestorsAndExcludesUnrelatedBranches()
    {
        using var databaseFile = new TestDatabase();
        databaseFile.Execute((connection, transaction) =>
        {
            var writer = new QuestDatabaseWriter(
                connection,
                transaction,
                new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase)
                {
                    ["records/items/a.dbr"] = 1
                });
            var quest = new QuestDefinition
            {
                Path = "quests/test/quest.qst",
                Source = "base",
                Uid = 10,
                Flags = 0,
                Name = "Test Quest",
                Region = "Test Region",
                Tasks = []
            };
            var questPk = writer.InsertQuest(quest);
            var conversation = new ConversationDefinition
            {
                Path = "conversations/test.cnv",
                Root = _step(0, "node",
                [
                    _step(1, "link",
                    [
                        _step(2, "accept", actions:
                        [
                            new ScriptableValue
                            {
                                Kind = "BeginQuest",
                                QuestPath = quest.Path
                            }
                        ])
                    ], linkId: 42),
                    _step(3, "speech")
                ])
            };

            ConversationQuestImporter.Import(
                writer,
                [conversation],
                new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase)
                {
                    [quest.Path] = questPk
                },
                new Dictionary<string, List<long>>(StringComparer.OrdinalIgnoreCase)
                {
                    [conversation.Path] = [1]
                });
        });
        using var database = new CliDatabase(databaseFile.Path);

        var quests = database.Quests.LoadMatches("Test Quest", true, 0, 1);
        database.Quests.PopulateDetails(quests);

        var result = Assert.Single(quests);
        var nodes = result.Nodes ?? [];
        Assert.Equal(3, nodes.Count);
        Assert.Equal([0, 1, 2], nodes.Select(node => node.Ordinal));
        Assert.Null(nodes[0].ParentNodeId);
        Assert.Equal(nodes[0].NodeId, nodes[1].ParentNodeId);
        Assert.Equal(nodes[1].NodeId, nodes[2].ParentNodeId);
        Assert.Equal(42, nodes[1].LinkId);
        Assert.Equal("offer", Assert.Single(result.Entities ?? []).Role);
        Assert.Equal("begin", Assert.Single(result.Edges ?? []).Kind);
    }

    private static ConversationStep _step(
        int ordinal,
        string type,
        IReadOnlyList<ConversationStep>? children = null,
        IReadOnlyList<ScriptableValue>? actions = null,
        int? linkId = null)
    {
        return new ConversationStep
        {
            Ordinal = ordinal,
            Type = type,
            Flags = 0,
            LinkId = linkId,
            Conditions = new ScriptableGroup { Operator = "and", Values = [] },
            Actions = actions ?? [],
            Children = children ?? []
        };
    }
}
