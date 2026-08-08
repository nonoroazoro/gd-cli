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
}
