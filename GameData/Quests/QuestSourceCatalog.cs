using GdCli.GameData.Conversations;
using GdCli.GameData.Lua;

namespace GdCli.GameData.Quests;

internal sealed class QuestSourceCatalog
{
    public required IReadOnlyDictionary<string, QuestDefinition> Quests { get; init; }

    public required IReadOnlyDictionary<string, ConversationDefinition> Conversations { get; init; }

    public required IReadOnlyDictionary<string, LuaFunctionMetadata> LuaFunctions { get; init; }
}
