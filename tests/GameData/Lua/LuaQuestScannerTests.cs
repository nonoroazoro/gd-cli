using GdCli.GameData.Lua;

namespace GdCli.Tests.GameData.Lua;

public sealed class LuaQuestScannerTests
{
    [Fact]
    public void ScanResolvesFunctionLocalQuestConstantsAndStateAliases()
    {
        const string source = """
            local inventorStates = orderedTable()
            inventorStates[1] = { dbr = "records/creatures/npcs/inventor.dbr" }

            function Quest.onAddToWorld(objectId)
                local questId = 0x10
                local taskId = 0x20
                TokenStateBasedObjectSwap(objectId, inventorStates)
                GrantQuest(questId, taskId)
            end

            function Other.onAddToWorld(objectId)
                local questId = 48
                GrantQuest(questId, 64)
            end
            """;

        var functions = LuaQuestScanner.Scan(source);

        Assert.Equal(2, functions.Count);
        var quest = functions[0];
        Assert.Equal("Quest.onAddToWorld", quest.Name);
        Assert.Equal(["records/creatures/npcs/inventor.dbr"], quest.SpawnedRecordIds);
        var grant = Assert.Single(quest.QuestGrants);
        Assert.Equal(0x10u, grant.QuestUid);
        Assert.Equal(0x20u, grant.TaskUid);
        Assert.Equal(0x30u, Assert.Single(functions[1].QuestGrants).QuestUid);
        Assert.Equal(0x40u, Assert.Single(functions[1].QuestGrants).TaskUid);
    }
}
