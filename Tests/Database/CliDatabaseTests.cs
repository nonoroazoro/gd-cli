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

        Assert.Equal(1, info.MiRecordCount);
        Assert.Equal(1, info.MiNameTagCount);
        Assert.Equal(1, info.AscendedAffixCount);
        Assert.Equal(1, info.AscendedSkillModifierCount);
        Assert.Equal(["oneHandMelee"], info.AscendedCategories);
    }

    [Fact]
    public void LoadRecordNamesResolvesRecordsInOneQuerySurface()
    {
        using var fixture = new TestDatabase();
        using var database = new CliDatabase(fixture.Path);

        var names = database.LoadRecordNames(
        [
            "RECORDS/SKILLS/ITEMSKILLSGDX3/SKILLMODIFIERS/ASCENDED/A.DBR",
            "records/missing.dbr"
        ]);

        Assert.Single(names);
        Assert.Equal(
            "Skill Power",
            names["records/skills/itemskillsgdx3/skillmodifiers/ascended/a.dbr"]);
    }
}
