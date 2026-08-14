using GdCli.GameData.Binary;

namespace GdCli.GameData.Scriptables;

internal static class ScriptableReader
{
    private static readonly string[] _actionKinds =
    [
        "BeginQuest", "BeginQuestTask", "CompleteQuest", "CompleteQuestTask", "GiveMoney",
        "GiveFaction", "GiveItem", "GiveLevel", "GiveExperience", "GiveToken", "RemoveToken",
        "LuaScript", "DebugPrint", "PlaySound", "PlayAnimation", "GiveSkillPoint",
        "OpenMerchantWindow", "Notification", "GiveAttributePoint", "ScriptEvent", "GiveRandomItem",
        "GenerateRandomValue", "CastSkill", "UnlockFaction", "SetFaction", "GiveDevotion",
        "GiveTribute", "UnlockTutorial", "PlayVideo"
    ];

    private static readonly string[] _conditionKinds =
    [
        "OnQuest", "QuestTaskComplete", "HasExperience", "IsLevel", "IsDifficulty", "IsHardcore",
        "HasFaction", "HasToken", "HasMoney", "HasItem", "HasKilled", "HasQuestObjective",
        "LuaScript", "KnowsPlayer", "OnQuestTask", "UsedQuestItem", "HasKilledProxy",
        "WaitForCompletion", "ServerHasToken", "QuestBlocked", "AnyoneHasToken", "CheckRandomValue",
        "OwnsDLC", "HasTribute", "EarnedDevotion", "HasMastery", "IsChallenge", "HasBuff",
        "HasKilledMonsterClass", "HasKilledFaction"
    ];

    public static IReadOnlyList<ScriptableValue> ReadActions(GameBinaryReader reader)
    {
        var count = _readCount(reader, "action");
        var result = new List<ScriptableValue>(count);
        for (var index = 0; index < count; index++)
        {
            try
            {
                result.Add(_readAction(reader));
            }
            catch (GameDataException exception)
            {
                var previous = result.Count == 0 ? "none" : result[^1].Kind;
                throw new GameDataException(
                    $"Failed to read action {index + 1} of {count} at {reader.Position} after {previous}: {exception.Message}",
                    exception);
            }
        }
        return result;
    }

    public static ScriptableGroup ReadConditions(GameBinaryReader reader)
    {
        var count = _readCount(reader, "condition");
        if (count == 0)
            return new ScriptableGroup { Operator = "and", Values = [] };
        var groupOperator = reader.ReadInt32() switch
        {
            0 => "and",
            1 => "or",
            var value => throw new GameDataException($"Unsupported scriptable condition operator: {value}")
        };
        var result = new List<ScriptableValue>(count);
        for (var index = 0; index < count; index++)
            result.Add(_readCondition(reader));
        return new ScriptableGroup { Operator = groupOperator, Values = result };
    }

    private static ScriptableValue _readAction(GameBinaryReader reader)
    {
        var type = reader.ReadInt32();
        var kind = _kind(_actionKinds, type, "action");
        _ = reader.ReadByte();
        return type switch
        {
            0 or 2 => _value(kind, questPath: reader.ReadString()),
            1 or 3 => _value(kind, questPath: reader.ReadString(), taskUid: reader.ReadUInt32()),
            4 or 25 or 26 or 27 => _value(kind, numericValue: reader.ReadInt32()),
            5 or 24 => _value(kind, textValue: reader.ReadString(), numericValue: reader.ReadInt32()),
            6 or 20 => _value(kind, recordId: reader.ReadString(), numericValue: reader.ReadInt32()),
            7 or 8 or 14 or 15 or 18 => _value(kind, numericValue: reader.ReadUInt32()),
            9 or 10 => _value(kind, token: reader.ReadString()),
            11 => _value(kind, function: _readLua(reader)),
            12 or 17 or 19 => _value(kind, textValue: reader.ReadString()),
            13 or 22 => _value(kind, recordId: reader.ReadString()),
            16 => _value(kind),
            21 => _value(
                kind,
                textValue: reader.ReadString(),
                numericValue: reader.ReadInt32(),
                secondaryNumericValue: reader.ReadInt32()),
            23 => _value(kind, textValue: reader.ReadString()),
            28 => _value(kind, textValue: reader.ReadString(), booleanValue: reader.ReadBoolean()),
            _ => throw new GameDataException($"Unsupported scriptable action type: {type}")
        };
    }

    private static ScriptableValue _readCondition(GameBinaryReader reader)
    {
        var type = reader.ReadInt32();
        var kind = _kind(_conditionKinds, type, "condition");
        var version = reader.ReadByte();
        var comparison = reader.ReadInt32();
        return type switch
        {
            0 or 19 => _value(kind, comparison, questPath: reader.ReadString(), booleanValue: reader.ReadBoolean()),
            1 or 14 => _value(
                kind,
                comparison,
                questPath: reader.ReadString(),
                taskUid: reader.ReadUInt32(),
                booleanValue: reader.ReadBoolean()),
            2 or 3 or 8 or 22 or 23 or 24 or 26 => _value(
                kind,
                comparison,
                numericValue: reader.ReadUInt32()),
            4 => _value(kind, comparison, numericValue: reader.ReadUInt32()),
            25 => _value(
                kind,
                comparison,
                numericValue: reader.ReadUInt32(),
                booleanValue: reader.ReadBoolean()),
            28 => _value(
                kind,
                comparison,
                numericValue: reader.ReadUInt32(),
                secondaryNumericValue: reader.ReadUInt32(),
                tertiaryNumericValue: reader.ReadUInt32()),
            5 or 13 => _value(kind, comparison, booleanValue: reader.ReadBoolean()),
            6 => _readFactionCondition(reader, kind, version, comparison),
            7 or 18 or 20 => _value(
                kind,
                comparison,
                token: reader.ReadString(),
                booleanValue: reader.ReadBoolean()),
            27 => _value(
                kind,
                comparison,
                recordId: reader.ReadString(),
                booleanValue: reader.ReadBoolean()),
            9 => _value(
                kind,
                comparison,
                recordId: reader.ReadString(),
                numericValue: reader.ReadUInt32(),
                booleanValue: reader.ReadBoolean()),
            10 or 16 => _readRecordListCondition(reader, kind, version, comparison),
            11 => _readQuestObjectiveCondition(reader, kind, version, comparison),
            12 => _readLuaCondition(reader, kind, version, comparison),
            15 => _value(
                kind,
                comparison,
                recordId: reader.ReadString(),
                numericValue: reader.ReadUInt32()),
            17 => _value(kind, comparison),
            21 => _value(
                kind,
                comparison,
                textValue: reader.ReadString(),
                numericValue: reader.ReadInt32()),
            29 => _value(
                kind,
                comparison,
                numericValue: reader.ReadInt32(),
                secondaryNumericValue: reader.ReadInt32(),
                tertiaryNumericValue: reader.ReadUInt32()),
            _ => throw new GameDataException($"Unsupported scriptable condition type: {type}")
        };
    }

    private static ScriptableValue _readFactionCondition(
        GameBinaryReader reader,
        string kind,
        int version,
        int comparison)
    {
        var faction = reader.ReadString();
        var value = version >= 1 ? reader.ReadInt32() : 0;
        return _value(kind, comparison, textValue: faction, numericValue: value);
    }

    private static ScriptableValue _readRecordListCondition(
        GameBinaryReader reader,
        string kind,
        int version,
        int comparison)
    {
        var count = _readCount(reader, "record");
        var records = new List<string>(count);
        for (var index = 0; index < count; index++)
            records.Add(_normalizePath(reader.ReadString()));
        return _value(
            kind,
            comparison,
            recordIds: records,
            numericValue: reader.ReadUInt32());
    }

    private static ScriptableValue _readQuestObjectiveCondition(
        GameBinaryReader reader,
        string kind,
        int version,
        int comparison)
    {
        var quest = reader.ReadString();
        var task = reader.ReadUInt32();
        if (version < 1)
            return _value(kind, comparison, questPath: quest, taskUid: task);
        return _value(
            kind,
            comparison,
            questPath: quest,
            taskUid: task,
            objectiveUid: reader.ReadUInt32(),
            booleanValue: reader.ReadBoolean());
    }

    private static ScriptableValue _readLuaCondition(
        GameBinaryReader reader,
        string kind,
        int version,
        int comparison)
    {
        var function = _readLua(reader);
        var returnComparison = reader.ReadInt32();
        var returnType = reader.ReadInt32();
        var boolValue = reader.ReadBoolean();
        var numberValue = reader.ReadSingle();
        var stringValue = reader.ReadString();
        return _value(
            kind,
            comparison,
            function: function,
            textValue: stringValue,
            numericValue: numberValue,
            secondaryNumericValue: returnComparison,
            booleanValue: returnType == 0 ? boolValue : null);
    }

    private static string _readLua(GameBinaryReader reader)
    {
        var function = reader.ReadString();
        var count = _readCount(reader, "Lua argument");
        for (var index = 0; index < count; index++)
        {
            _ = reader.ReadInt32();
            _ = reader.ReadBoolean();
            _ = reader.ReadSingle();
            _ = reader.ReadString();
        }
        return function;
    }

    private static ScriptableValue _value(
        string kind,
        int? comparison = null,
        string? questPath = null,
        uint? taskUid = null,
        uint? objectiveUid = null,
        string? recordId = null,
        IReadOnlyList<string>? recordIds = null,
        string? token = null,
        string? function = null,
        string? textValue = null,
        double? numericValue = null,
        double? secondaryNumericValue = null,
        double? tertiaryNumericValue = null,
        bool? booleanValue = null)
    {
        return new ScriptableValue
        {
            Kind = kind,
            Comparison = comparison,
            QuestPath = questPath == null ? null : _normalizePath(questPath),
            TaskUid = taskUid,
            ObjectiveUid = objectiveUid,
            RecordId = recordId == null ? null : _normalizePath(recordId),
            RecordIds = recordIds ?? [],
            Token = token,
            Function = function,
            TextValue = textValue,
            NumericValue = numericValue,
            SecondaryNumericValue = secondaryNumericValue,
            TertiaryNumericValue = tertiaryNumericValue,
            BooleanValue = booleanValue
        };
    }

    private static int _readCount(GameBinaryReader reader, string subject)
    {
        var count = reader.ReadInt32();
        if (count is < 0 or > 1_000_000)
            throw new GameDataException($"Invalid {subject} count: {count}");
        return count;
    }

    private static string _kind(string[] kinds, int type, string subject)
    {
        if (type < 0 || type >= kinds.Length)
            throw new GameDataException($"Unsupported scriptable {subject} type: {type}");
        return kinds[type];
    }

    private static string _normalizePath(string value) => value.Replace('\\', '/').ToLowerInvariant();
}
