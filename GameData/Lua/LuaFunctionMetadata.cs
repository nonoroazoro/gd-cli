namespace GdCli.GameData.Lua;

internal sealed class LuaFunctionMetadata
{
    public required string Name { get; init; }

    public required IReadOnlyList<string> SpawnedRecordIds { get; init; }

    public required IReadOnlyList<LuaQuestGrant> QuestGrants { get; init; }
}
