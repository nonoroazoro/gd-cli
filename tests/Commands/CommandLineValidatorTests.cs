using GdCli.Commands;

namespace GdCli.Tests.Commands;

public sealed class CommandLineValidatorTests
{
    [Fact]
    public void ValidateAllowsMiOnlyForItemsAndItemFamilies()
    {
        var items = CommandLineParser.Parse(["items", "--mi", "true"]);
        var families = CommandLineParser.Parse(["item-families", "--mi", "false"]);

        CommandLineValidator.Validate(items);
        CommandLineValidator.Validate(families);

        var affixes = CommandLineParser.Parse(["affixes", "--mi", "true"]);
        Assert.Throws<CommandLineException>(() => CommandLineValidator.Validate(affixes));
        var invalidFamilyFilter = CommandLineParser.Parse(["item-families", "--rarity", "Rare"]);
        Assert.Throws<CommandLineException>(() => CommandLineValidator.Validate(invalidFamilyFilter));
    }

    [Fact]
    public void ValidateScopesCompatibilityFiltersToTheirCommands()
    {
        var affixes = CommandLineParser.Parse(["affixes", "--type", "WeaponMelee_Mace"]);
        var ascended = CommandLineParser.Parse(
            ["ascended-affixes", "--category", "oneHandMelee"]);

        CommandLineValidator.Validate(affixes);
        CommandLineValidator.Validate(ascended);

        Assert.Throws<CommandLineException>(() => CommandLineValidator.Validate(
            CommandLineParser.Parse(["items", "--category", "armor"])));
        Assert.Throws<CommandLineException>(() => CommandLineValidator.Validate(
            CommandLineParser.Parse(["ascended-affixes", "--type", "WeaponMelee_Mace"])));
        Assert.Throws<CommandLineException>(() => CommandLineValidator.Validate(
            CommandLineParser.Parse(["ascended-affixes", "--rarity", "Rare"])));
    }
}
