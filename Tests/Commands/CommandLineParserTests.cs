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
    public void ParseReadsUnifiedAffixFilters()
    {
        var standard = CommandLineParser.Parse(
            ["affixes", "of", "Fervor", "--family", "standard", "--type", "WeaponMelee_Mace", "--kind", "prefix"]);
        var ascended = CommandLineParser.Parse(
            ["affixes", "--family", "ascended", "--category", "oneHandMelee"]);

        Assert.Equal("of Fervor", standard.AffixQuery);
        Assert.Equal("standard", standard.AffixFamily);
        Assert.Equal("WeaponMelee_Mace", standard.ItemClass);
        Assert.Equal("prefix", standard.Kind);
        Assert.Equal("ascended", ascended.AffixFamily);
        Assert.Equal("oneHandMelee", ascended.AscendedCategory);
    }

    [Fact]
    public void ParseReadsMultiWordQuestName()
    {
        var options = CommandLineParser.Parse(["quests", "Into", "the", "Breach"]);

        Assert.Equal("Into the Breach", options.QuestQuery);
    }

    [Fact]
    public void ParseReadsMultiWordItemName()
    {
        var options = CommandLineParser.Parse(["items", "Conduit", "of", "Whispers"]);

        Assert.Equal("Conduit of Whispers", options.ItemQuery);
    }

    [Fact]
    public void ParseReadsFamilyGroupingAndAvailabilityAuditFilter()
    {
        var items = CommandLineParser.Parse(
            ["items", "--families", "--availability", "all"]);

        Assert.True(items.GroupFamilies);
        Assert.True(items.AvailabilitySpecified);
        Assert.Null(items.Availability);
    }
}
