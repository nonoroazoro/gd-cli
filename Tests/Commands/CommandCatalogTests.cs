using GdCli.Commands;

namespace GdCli.Tests.Commands;

public sealed class CommandCatalogTests
{
    [Fact]
    public void HelpAndGlobalOptionsHaveDistinctScopes()
    {
        Assert.DoesNotContain("help", CommandCatalog.CommandNames);
        Assert.Equal(["--query JMESPATH"], CommandCatalog.GlobalFlags);
        Assert.Equal(["--help", "-h"], CommandCatalog.HelpFlags);
        Assert.All(CommandCatalog.GetTree().Children, command =>
        {
            Assert.Contains("--help", command.Options);
            Assert.Contains("-h", command.Options);
        });
    }

    [Fact]
    public void RootCommandsExposeDistinctDomains()
    {
        Assert.Equal(
            ["affixes", "info", "init", "items", "quests", "schema", "tree"],
            CommandCatalog.CommandNames.Order(StringComparer.Ordinal).ToArray());

        var items = CommandCatalog.GetCommand(["items"]);
        var affixes = CommandCatalog.GetCommand(["affixes"]);
        Assert.Equal(["query (optional)"], items.Arguments);
        Assert.Contains(
            "--availability known|referenced|unresolved|unavailable|all",
            items.Options);
        Assert.Contains("--families", items.Options);
        Assert.Contains("--family standard|ascended|all", affixes.Options);
        Assert.Contains("--type VALUE|all", affixes.Options);
        Assert.Contains("--category VALUE|all", affixes.Options);
    }
}
