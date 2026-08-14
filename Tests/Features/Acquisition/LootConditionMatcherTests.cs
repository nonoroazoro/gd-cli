using GdCli.Features.Acquisition;

namespace GdCli.Tests.Features.Acquisition;

public sealed class LootConditionMatcherTests
{
    [Theory]
    [InlineData("lootName3", "lootWeight3", true)]
    [InlineData("lootName3", "lootChance3", true)]
    [InlineData("lootName3", "lootWeight2", false)]
    [InlineData("lootHeadItem2", "chanceToEquipHeadItem2", true)]
    [InlineData("lootHeadItem2", "chanceToEquipHead", true)]
    [InlineData("pool4", "weight4", true)]
    [InlineData("nameChampion2", "weightChampion2", true)]
    [InlineData("nameChampion2", "weight2", false)]
    [InlineData("poolLegendary1", "weightLegendary1", true)]
    [InlineData("poolLegendary1", "weight1", false)]
    [InlineData("name1", "chanceToRun", true)]
    [InlineData("lootHeadItem2", "charLevel", true)]
    public void IsMatchSelectsOnlyConditionsForTheReferenceSlot(
        string referenceField,
        string conditionField,
        bool expected)
    {
        Assert.Equal(expected, LootConditionMatcher.IsMatch(referenceField, conditionField));
    }
}
