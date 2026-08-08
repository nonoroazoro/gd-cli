namespace GdCli.Features.Drops;

internal static class DropConditionMatcher
{
    public static bool IsMatch(string referenceField, string conditionField)
    {
        if (conditionField is "forceHighestLevel" or "minItemLevelEquation" or "maxItemLevelEquation" or "chanceToRun")
            return true;
        var slot = _trailingDigits(referenceField);
        if (referenceField.StartsWith("lootName", StringComparison.OrdinalIgnoreCase))
            return conditionField.Equals($"lootWeight{slot}", StringComparison.OrdinalIgnoreCase) ||
                   conditionField.Equals($"lootChance{slot}", StringComparison.OrdinalIgnoreCase);
        if (referenceField.StartsWith("name", StringComparison.OrdinalIgnoreCase))
            return conditionField.Equals($"weight{slot}", StringComparison.OrdinalIgnoreCase) ||
                   conditionField.Equals($"levelVarianceEquation{slot}", StringComparison.OrdinalIgnoreCase);
        if (referenceField.StartsWith("pool", StringComparison.OrdinalIgnoreCase))
            return conditionField.Equals($"weight{slot}", StringComparison.OrdinalIgnoreCase);
        var itemIndex = referenceField.LastIndexOf("Item", StringComparison.OrdinalIgnoreCase);
        if (referenceField.StartsWith("loot", StringComparison.OrdinalIgnoreCase) && itemIndex > 4)
        {
            var equipmentGroup = referenceField[4..itemIndex];
            return conditionField.Equals($"chanceToEquip{equipmentGroup}", StringComparison.OrdinalIgnoreCase) ||
                   conditionField.Equals($"chanceToEquip{equipmentGroup}Item{slot}", StringComparison.OrdinalIgnoreCase);
        }
        return false;
    }

    private static string _trailingDigits(string value)
    {
        var start = value.Length;
        while (start > 0 && char.IsDigit(value[start - 1]))
            start--;
        return value[start..];
    }
}
