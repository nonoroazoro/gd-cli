using GdCli.Database;

namespace GdCli.Tests.Database;

public sealed class AffixRepositoryTests
{
    [Fact]
    public void QueriesStandardAffixesByFamilyKindAndExactName()
    {
        using var fixture = new TestDatabase();
        using var database = new CliDatabase(fixture.Path);
        var filter = new AffixFilter(
            "standard",
            "Rare",
            "prefix",
            null,
            null,
            null,
            null,
            "Balanced",
            true);

        Assert.Equal(1, database.Affixes.Count(filter));
        var affix = Assert.Single(database.Affixes.Load(filter, 0, 1));
        Assert.Equal("Balanced", affix.Name);
        Assert.Equal("standard", affix.Family);
    }

    [Fact]
    public void QueriesFilterByCompatibleItemClassInSql()
    {
        using var fixture = new TestDatabase();
        using var database = new CliDatabase(fixture.Path);

        var result = database.Affixes.Load(
            new AffixFilter(null, null, null, "Mace", null, null, null),
            0,
            null);

        Assert.Equal("Balanced", Assert.Single(result).Name);
    }

    [Fact]
    public void QueriesAscendedAffixesByGameCategory()
    {
        using var fixture = new TestDatabase();
        using var database = new CliDatabase(fixture.Path);
        var filter = new AffixFilter(
            "ascended",
            null,
            null,
            null,
            "oneHandMelee",
            null,
            null);

        var affix = Assert.Single(database.Affixes.Load(filter, 0, null));
        Assert.Equal("ascended", affix.Family);
        Assert.Equal(["oneHandMelee"], affix.Categories);
        Assert.Equal(["affix"], affix.Groups);
        Assert.Single(database.RecordSkillModifiers.Load([affix.RecordId])[affix.RecordId]);
    }

    [Fact]
    public void TreatsQueryWildcardsAsLiteralCharacters()
    {
        using var fixture = new TestDatabase();
        fixture.Execute("UPDATE records SET display_name = '100% Sharp' WHERE id = 6");
        using var database = new CliDatabase(fixture.Path);
        var filter = new AffixFilter(
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            "100%",
            false);

        Assert.Equal(1, database.Affixes.Count(filter));
        Assert.Equal("100% Sharp", Assert.Single(database.Affixes.Load(filter, 0, null)).Name);
    }
}
