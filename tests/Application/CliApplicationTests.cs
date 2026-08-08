using System.Globalization;
using System.Text.Json;
using GdCli.Application;

namespace GdCli.Tests.Application;

public sealed class CliApplicationTests
{
    [Fact]
    public void InvalidCommandWritesJsonOnlyToStderr()
    {
        using var standardOutput = new StringWriter(CultureInfo.InvariantCulture);
        using var standardError = new StringWriter(CultureInfo.InvariantCulture);
        var application = new CliApplication(standardOutput, standardError);

        var exitCode = application.Run(["invalid-command"]);

        Assert.NotEqual(0, exitCode);
        Assert.Equal(string.Empty, standardOutput.ToString());
        using var error = JsonDocument.Parse(standardError.ToString());
        Assert.Equal("invalid_arguments", error.RootElement.GetProperty("code").GetString());
        Assert.Equal(exitCode, error.RootElement.GetProperty("exitCode").GetInt32());
    }

    [Fact]
    public void InvalidOutputQueryWritesJsonOnlyToStderr()
    {
        using var standardOutput = new StringWriter(CultureInfo.InvariantCulture);
        using var standardError = new StringWriter(CultureInfo.InvariantCulture);
        var application = new CliApplication(standardOutput, standardError);

        var exitCode = application.Run(["--help", "--query", "["]);

        Assert.NotEqual(0, exitCode);
        Assert.Equal(string.Empty, standardOutput.ToString());
        using var error = JsonDocument.Parse(standardError.ToString());
        Assert.Equal("invalid_arguments", error.RootElement.GetProperty("code").GetString());
    }

    [Fact]
    public void CommandHelpDoesNotExecuteTheCommand()
    {
        using var standardOutput = new StringWriter(CultureInfo.InvariantCulture);
        using var standardError = new StringWriter(CultureInfo.InvariantCulture);
        var application = new CliApplication(standardOutput, standardError);

        var exitCode = application.Run(["init", "missing", "--game-language", "en", "--help"]);

        Assert.Equal(0, exitCode);
        Assert.Equal(string.Empty, standardError.ToString());
        using var help = JsonDocument.Parse(standardOutput.ToString());
        Assert.Equal("init", help.RootElement.GetProperty("command").GetString());
        Assert.Equal(4, help.RootElement.EnumerateObject().Count());
        Assert.Contains("grim-dawn-game-directory", help.RootElement
            .GetProperty("arguments")
            .EnumerateArray()
            .Select(argument => argument.GetString()));
    }

    [Fact]
    public void RootHelpReturnsTheCompleteCommandList()
    {
        using var standardOutput = new StringWriter(CultureInfo.InvariantCulture);
        using var standardError = new StringWriter(CultureInfo.InvariantCulture);
        var application = new CliApplication(standardOutput, standardError);

        var exitCode = application.Run(["--help"]);

        Assert.Equal(0, exitCode);
        Assert.Equal(string.Empty, standardError.ToString());
        using var help = JsonDocument.Parse(standardOutput.ToString());
        Assert.Equal(["--query JMESPATH"], help.RootElement
            .GetProperty("globalFlags")
            .EnumerateArray()
            .Select(option => option.GetString()));
        Assert.Equal(2, help.RootElement.EnumerateObject().Count());
        Assert.Contains("init", help.RootElement
            .GetProperty("commands")
            .EnumerateArray()
            .Select(command => command.GetProperty("name").GetString()));
    }
}
