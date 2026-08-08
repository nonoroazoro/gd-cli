using GdCli.Commands;

namespace GdCli.Contracts;

internal sealed class HelpResponse
{
    public IReadOnlyList<CommandSummary> Commands { get; init; } = CommandCatalog.GetTree().Children
        .Select(command => new CommandSummary
        {
            Name = command.Name,
            Description = command.Description
        })
        .ToList();

    public IReadOnlyList<string> GlobalFlags { get; init; } = CommandCatalog.GlobalFlags;
}
