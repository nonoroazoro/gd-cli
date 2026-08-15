using GdCli.Database;

namespace GdCli.Tests.Database;

public sealed class QuestRepositoryTests
{
    [Fact]
    public void LoadDetailsReturnsGraphAndDeduplicatedKeyLocations()
    {
        using var databaseFile = new TestDatabase();
        databaseFile.Execute("""
            INSERT INTO records(id, record_id, class, display_name) VALUES
                (100, 'records/creatures/npcs/yurgryn.dbr', 'NPC', 'Yurgryn'),
                (101, 'records/scriptentities/yurgryn_state.dbr', 'ScriptEntity', ''),
                (102, 'records/ui/riftgatemap/locations/test.dbr', 'Map', '');
            INSERT INTO quests(id, quest_path, source_name, uid, flags, region, name) VALUES
                (1, 'quests/test/breach.qst', 'gdx3', 10, 0, 'Test', 'Into the Breach');
            INSERT INTO quest_nodes(
                id, quest_pk, parent_pk, ordinal, kind, phase, uid, name, description,
                flags, condition_operator, origin_path) VALUES
                (1, 1, NULL, 0, 'task', 'task', 20, 'Find Yurgryn', '', 0, 'and', 'quests/test/breach.qst'),
                (2, 1, 1, 0, 'conversation', 'accept', NULL, '', '', 0, 'and', 'conversations/test.cnv');
            INSERT INTO quest_edges(
                id, quest_pk, source_node_pk, target_quest_path, target_task_uid, kind, origin_path) VALUES
                (1, 1, 2, 'quests/test/breach.qst', 20, 'begin', 'conversations/test.cnv');
            INSERT INTO quest_entities(id, quest_pk, node_pk, record_pk, role, origin_path) VALUES
                (1, 1, 1, 100, 'participant', 'quests/test/breach.qst'),
                (2, 1, 2, 100, 'participant', 'conversations/test.cnv');
            INSERT INTO entity_aliases(alias_pk, placed_pk, origin_path) VALUES
                (100, 101, 'Quest.onAddToWorld');
            INSERT INTO levels(id, source_name, level_path, rift_gate_record_id) VALUES
                (1, 'gdx3', 'levels/test.lvl', 'records/ui/riftgatemap/locations/test.dbr');
            INSERT INTO placements(level_pk, entity_ordinal, record_pk, world_x, world_y, world_z) VALUES
                (1, 0, 101, 10.5, 20.5, 30.5);
            """);
        using var database = new CliDatabase(databaseFile.Path);

        var quests = database.Quests.LoadMatches("Into the Breach", true, 0, 25);
        database.Quests.PopulateDetails(quests);

        var quest = Assert.Single(quests);
        Assert.Equal(2, quest.NodeCount);
        Assert.Single(quest.Edges ?? []);
        var entity = Assert.Single(quest.Entities ?? []);
        Assert.Equal([1, 2], entity.NodeIds);
        Assert.Equal(2, entity.Origins.Count);
        var location = Assert.Single(entity.Locations);
        Assert.Equal("scriptState", location.Resolution);
        Assert.Equal("levels/test.lvl", location.Level);
        Assert.Equal(10.5, location.X);
        Assert.Equal(20.5, location.Y);
        Assert.Equal(30.5, location.Z);
    }
}
