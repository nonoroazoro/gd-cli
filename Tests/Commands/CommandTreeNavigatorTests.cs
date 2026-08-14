using GdCli.Commands;

namespace GdCli.Tests.Commands;

public sealed class CommandTreeNavigatorTests
{
    [Fact]
    public void ResolveTraversesNestedCommandPaths()
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

        Assert.Same(group, CommandTreeNavigator.Resolve(root, ["group"]));
        Assert.Same(leaf, CommandTreeNavigator.Resolve(root, ["group", "leaf"]));
        Assert.Null(CommandTreeNavigator.FindChild(group, "missing"));
    }
}
