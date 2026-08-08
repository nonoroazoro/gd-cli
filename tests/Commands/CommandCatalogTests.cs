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
    public void AffixCommandsExposeSeparateCompatibilityFilters()
    {
        var affixes = CommandCatalog.GetCommand(["affixes"]);
        var ascended = CommandCatalog.GetCommand(["ascended-affixes"]);

        Assert.Contains("--type VALUE|all", affixes.Options);
        Assert.DoesNotContain(affixes.Options, option => option.StartsWith("--category", StringComparison.Ordinal));
        Assert.Contains("--category VALUE|all", ascended.Options);
        Assert.DoesNotContain(ascended.Options, option => option.StartsWith("--type", StringComparison.Ordinal));
    }
}
