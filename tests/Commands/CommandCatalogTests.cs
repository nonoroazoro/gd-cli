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

}
