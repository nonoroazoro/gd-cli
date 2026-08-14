using GdCli.Features.Drops;

namespace GdCli.Tests.Features.Drops;

public sealed class DropConditionMatcherTests
{
    [Theory]
    [InlineData("lootName3", "lootWeight3", true)]
    [InlineData("lootName3", "lootChance3", true)]
    [InlineData("lootName3", "lootWeight2", false)]
    [InlineData("lootHeadItem2", "chanceToEquipHeadItem2", true)]
    [InlineData("lootHeadItem2", "chanceToEquipHead", true)]
    [InlineData("pool4", "weight4", true)]
    [InlineData("name1", "chanceToRun", true)]
    public void IsMatchSelectsOnlyConditionsForTheReferenceSlot(
        string referenceField,
        string conditionField,
        bool expected)
    {
        Assert.Equal(expected, DropConditionMatcher.IsMatch(referenceField, conditionField));
    }
}
