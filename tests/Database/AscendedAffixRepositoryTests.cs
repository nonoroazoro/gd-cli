using GdCli.Database;

namespace GdCli.Tests.Database;

public sealed class AscendedAffixRepositoryTests
{
    [Fact]
    public void QueriesFilterByGameCategoryInSql()
    {
        using var fixture = new TestDatabase();
        using var database = new CliDatabase(fixture.Path);
        var filter = new AscendedAffixFilter("oneHandMelee");

        Assert.Equal(1, database.AscendedAffixes.Count(filter));
        var affix = Assert.Single(database.AscendedAffixes.Load(filter, 0, null));
        Assert.Equal(["oneHandMelee"], affix.Categories);
        Assert.Equal(["affix"], affix.Groups);
        Assert.Single(database.AscendedAffixes.LoadSkillModifiers([affix.RecordId])[affix.RecordId]);
    }
}
