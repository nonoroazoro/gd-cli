using GdCli.GameData.Lua;

namespace GdCli.Database;

internal static class LuaQuestImporter
{
    public static void Import(
        QuestDatabaseWriter writer,
        IEnumerable<LuaFunctionMetadata> functions,
        IReadOnlyDictionary<string, long> questPks,
        IReadOnlyDictionary<uint, string> questUids,
        IReadOnlyDictionary<string, List<long>> scriptBindings)
    {
        foreach (var function in functions)
        {
            if (!scriptBindings.TryGetValue(function.Name, out var bindings))
                continue;
            _insertAliases(writer, function, bindings);
            foreach (var grant in function.QuestGrants)
            {
                if (!questUids.TryGetValue(grant.QuestUid, out var questPath) ||
                    !questPks.TryGetValue(questPath, out var questPk))
                    continue;
                var nodePk = writer.InsertNode(
                    questPk,
                    null,
                    0,
                    "script",
                    "trigger",
                    null,
                    null,
                    null,
                    null,
                    string.Empty,
                    string.Empty,
                    0,
                    "and",
                    function.Name);
                writer.InsertEdge(
                    questPk,
                    nodePk,
                    questPath,
                    grant.TaskUid,
                    "begin",
                    function.Name);
                foreach (var binding in bindings)
                    writer.InsertEntity(questPk, nodePk, binding, "trigger", function.Name);
            }
        }
    }

    private static void _insertAliases(
        QuestDatabaseWriter writer,
        LuaFunctionMetadata function,
        IReadOnlyList<long> bindings)
    {
        foreach (var spawnedRecord in function.SpawnedRecordIds)
        {
            if (!writer.TryGetRecord(spawnedRecord, out var aliasPk))
                continue;
            foreach (var placedPk in bindings)
                writer.InsertAlias(aliasPk, placedPk, function.Name);
        }
    }
}
