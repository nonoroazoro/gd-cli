using System.Text.Json.Serialization;

namespace GdCli.Contracts;

internal sealed class CommandHelpResponse
{
    public required string Command { get; init; }

    public required string Description { get; init; }

    public required IReadOnlyList<string> Arguments { get; init; }

    public required IReadOnlyList<string> Options { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<CommandSummary>? Commands { get; init; }
}
