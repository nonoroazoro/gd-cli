using GdCli.Database;
using GdCli.GameData.Quests;
using GdCli.GameData.Scriptables;

namespace GdCli.Tests.Database;

public sealed class QuestDefinitionImporterTests
{
    [Fact]
    public void ImportPreservesTaskPhasesAndObjectiveTargets()
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
            QuestDefinitionImporter.Import(writer,
            [
                new QuestDefinition
                {
                    Path = "quests/test/quest.qst",
                    Source = "base",
                    Uid = 10,
                    Flags = 0,
                    Region = "Test Region",
                    Name = "Test Quest",
                    Tasks =
                    [
                        new QuestTask
                        {
                            Ordinal = 0,
                            Uid = 20,
                            Flags = 0,
                            IsBlocker = false,
                            DontPropagate = false,
                            OnAccept = [_event("onAccept", "Accepted")],
                            Objectives =
                            [
                                new QuestObjective
                                {
                                    Ordinal = 0,
                                    Uid = 30,
                                    Flags = 0,
                                    Name = "Find the target",
                                    Conditions = new ScriptableGroup
                                    {
                                        Operator = "and",
                                        Values =
                                        [
                                            new ScriptableValue
                                            {
                                                Kind = "HasItem",
                                                RecordId = "records/items/a.dbr"
                                            }
                                        ]
                                    },
                                    Actions =
                                    [
                                        new ScriptableValue
                                        {
                                            Kind = "CompleteQuestTask",
                                            QuestPath = "quests/test/quest.qst",
                                            TaskUid = 20
                                        }
                                    ]
                                }
                            ],
                            OnComplete = [_event("onComplete", "Completed")]
                        }
                    ]
                }
            ]);
        });
        using var database = new CliDatabase(databaseFile.Path);

        var quests = database.Quests.LoadMatches("Test Quest", true, 0, 1);
        database.Quests.PopulateDetails(quests);

        var quest = Assert.Single(quests);
        var nodes = quest.Nodes ?? [];
        Assert.Equal(["task", "event", "objective", "event"], nodes.Select(node => node.Kind));
        Assert.Equal(["task", "onAccept", "objective", "onComplete"], nodes.Select(node => node.Phase));
        Assert.False(nodes[0].IsBlocker);
        Assert.False(nodes[0].DontPropagate);
        Assert.All(nodes.Skip(1), node => Assert.Equal(nodes[0].NodeId, node.ParentNodeId));
        Assert.Equal("target", Assert.Single(quest.Entities ?? []).Role);
        Assert.Equal("completeTask", Assert.Single(quest.Edges ?? []).Kind);
    }

    private static QuestEvent _event(string phase, string name)
    {
        return new QuestEvent
        {
            Phase = phase,
            Ordinal = 0,
            Flags = 0,
            Name = name,
            Conditions = new ScriptableGroup { Operator = "and", Values = [] },
            Actions = []
        };
    }
}
