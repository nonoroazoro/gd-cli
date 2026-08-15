using GdCli.Commands;

namespace GdCli.Tests.Commands;

public sealed class CommandLineValidatorTests
{
    [Fact]
    public void ValidateScopesItemFiltersToItems()
    {
        var items = CommandLineParser.Parse(["items", "--mi", "true"]);
        CommandLineValidator.Validate(items);

        var affixes = CommandLineParser.Parse(["affixes", "--mi", "true"]);
        Assert.Throws<CommandLineException>(() => CommandLineValidator.Validate(affixes));
        Assert.Throws<CommandLineException>(() => CommandLineValidator.Validate(
            CommandLineParser.Parse(["items", "--families", "--rarity", "Rare"])));
    }

    [Fact]
    public void ValidateScopesCompatibilityFiltersToTheirCommands()
    {
        var affixes = CommandLineParser.Parse(["affixes", "--type", "WeaponMelee_Mace"]);
        var ascended = CommandLineParser.Parse(
            ["affixes", "--family", "ascended", "--category", "oneHandMelee"]);

        CommandLineValidator.Validate(affixes);
        CommandLineValidator.Validate(ascended);

        Assert.Throws<CommandLineException>(() => CommandLineValidator.Validate(
            CommandLineParser.Parse(["items", "--category", "armor"])));
        Assert.Throws<CommandLineException>(() => CommandLineValidator.Validate(
            CommandLineParser.Parse(["affixes", "--families"])));
        Assert.Throws<CommandLineException>(() => CommandLineValidator.Validate(
            CommandLineParser.Parse(
                ["affixes", "--family", "standard", "--category", "oneHandMelee"])));
        Assert.Throws<CommandLineException>(() => CommandLineValidator.Validate(
            CommandLineParser.Parse(
                ["affixes", "--family", "ascended", "--type", "WeaponMelee_Mace"])));
        Assert.Throws<CommandLineException>(() => CommandLineValidator.Validate(
            CommandLineParser.Parse(
                ["affixes", "--kind", "prefix", "--category", "oneHandMelee"])));
        Assert.Throws<CommandLineException>(() => CommandLineValidator.Validate(
            CommandLineParser.Parse(
                ["affixes", "--type", "WeaponMelee_Mace", "--category", "oneHandMelee"])));
    }

    [Fact]
    public void ValidateScopesAvailabilityToItemCatalogCommands()
    {
        CommandLineValidator.Validate(CommandLineParser.Parse(
            ["items", "--availability", "unresolved"]));
        Assert.Throws<CommandLineException>(() => CommandLineValidator.Validate(
            CommandLineParser.Parse(["affixes", "--availability", "known"])));
    }
}
