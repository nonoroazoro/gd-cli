using GdCli.Commands;

namespace GdCli.Tests.Commands;

public sealed class CommandLineParserTests
{
    [Fact]
    public void ParseResolvesHelpAtEveryCommandDepth()
    {
        var leaf = new CommandNode
        {
            Name = "leaf",
            Kind = "command",
            Description = "Leaf command."
        };
        var group = new CommandNode
        {
            Name = "group",
            Kind = "group",
            Description = "Command group.",
            Children = [leaf]
        };
        var root = new CommandNode
        {
            Name = "test",
            Kind = "root",
            Description = "Test root.",
            Children = [group]
        };

        var rootHelp = CommandLineParser.Parse(["--help"], root);
        Assert.Empty(rootHelp.CommandPath);
        Assert.True(rootHelp.HelpRequested);

        var groupHelp = CommandLineParser.Parse(["group", "--help"], root);
        Assert.Equal(["group"], groupHelp.CommandPath);
        Assert.True(groupHelp.HelpRequested);

        var leafHelp = CommandLineParser.Parse(["group", "leaf", "--help"], root);
        Assert.Equal(["group", "leaf"], leafHelp.CommandPath);
        Assert.True(leafHelp.HelpRequested);

        Assert.Throws<CommandLineException>(() =>
            CommandLineParser.Parse(["group", "missing", "--help"], root));
    }

    [Fact]
    public void ParseRequiresExplicitBooleanMiValue()
    {
        var options = CommandLineParser.Parse(["items", "--mi", "true"]);

        Assert.True(options.IsMi);
        Assert.True(options.MiSpecified);
        Assert.Throws<CommandLineException>(() => CommandLineParser.Parse(["items", "--mi", "yes"]));
    }

    [Fact]
    public void ParseReadsAffixCompatibilityAndAscendedCategoryFilters()
    {
        var affixes = CommandLineParser.Parse(["affixes", "--type", "WeaponMelee_Mace"]);
        var ascended = CommandLineParser.Parse(
            ["ascended-affixes", "--category", "oneHandMelee"]);

        Assert.Equal("WeaponMelee_Mace", affixes.ItemClass);
        Assert.Equal("oneHandMelee", ascended.AscendedCategory);
    }

    [Fact]
    public void ParseReadsMultiWordQuestName()
    {
        var options = CommandLineParser.Parse(["quest", "Into", "the", "Breach"]);

        Assert.Equal("Into the Breach", options.QuestQuery);
    }
}
