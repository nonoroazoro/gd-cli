using GdCli.Database;

namespace GdCli.Tests.Database;

public sealed class CliDatabaseTests
{
    [Fact]
    public void InfoDistinguishesMiRecordAndNameTagCounts()
    {
        using var fixture = new TestDatabase();
        using var database = new CliDatabase(fixture.Path);

        var info = database.GetInfo();

        Assert.Equal(1, info.MiCount);
        Assert.Equal(1, info.MiRecordCount);
        Assert.Equal(1, info.MiNameTagCount);
        Assert.Equal(1, info.AscendedAffixCount);
        Assert.Equal(1, info.AscendedSkillModifierCount);
        Assert.Equal(["oneHandMelee"], info.AscendedCategories);
    }
}
