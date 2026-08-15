namespace GdCli.Features.Acquisition;

internal static class LootConditionMatcher
{
    public static bool IsMatch(string referenceField, string conditionField)
    {
        if (conditionField is "forceHighestLevel" or "minItemLevelEquation" or
            "maxItemLevelEquation" or "chanceToRun" or "charLevel")
            return true;
        if (referenceField.StartsWith("lootName", StringComparison.OrdinalIgnoreCase))
        {
            var suffix = referenceField["lootName".Length..];
            return conditionField.Equals($"lootWeight{suffix}", StringComparison.OrdinalIgnoreCase) ||
                   conditionField.Equals($"lootChance{suffix}", StringComparison.OrdinalIgnoreCase);
        }
        if (referenceField.StartsWith("loot", StringComparison.OrdinalIgnoreCase))
        {
            var nameIndex = referenceField.IndexOf("Name", StringComparison.OrdinalIgnoreCase);
            if (nameIndex > "loot".Length)
            {
                var group = referenceField["loot".Length..nameIndex];
                var choice = referenceField[(nameIndex + "Name".Length)..];
                return conditionField.Equals($"loot{group}Chance", StringComparison.OrdinalIgnoreCase) ||
                       conditionField.Equals($"loot{group}Weight{choice}", StringComparison.OrdinalIgnoreCase);
            }
        }

        if (referenceField.StartsWith("name", StringComparison.OrdinalIgnoreCase))
        {
            var suffix = referenceField["name".Length..];
            return conditionField.Equals($"weight{suffix}", StringComparison.OrdinalIgnoreCase) ||
                   conditionField.Equals($"levelVarianceEquation{suffix}", StringComparison.OrdinalIgnoreCase);
        }
        if (referenceField.StartsWith("pool", StringComparison.OrdinalIgnoreCase))
        {
            var suffix = referenceField["pool".Length..];
            return conditionField.Equals($"weight{suffix}", StringComparison.OrdinalIgnoreCase);
        }
        var itemIndex = referenceField.LastIndexOf("Item", StringComparison.OrdinalIgnoreCase);
        if (referenceField.StartsWith("loot", StringComparison.OrdinalIgnoreCase) && itemIndex > 4)
        {
            var equipmentGroup = referenceField[4..itemIndex];
            var slot = referenceField[(itemIndex + "Item".Length)..];
            return conditionField.Equals($"chanceToEquip{equipmentGroup}", StringComparison.OrdinalIgnoreCase) ||
                   conditionField.Equals($"chanceToEquip{equipmentGroup}Item{slot}", StringComparison.OrdinalIgnoreCase);
        }

        return false;
    }
}
