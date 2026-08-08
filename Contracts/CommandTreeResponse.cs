using GdCli.Commands;

namespace GdCli.Contracts;

internal sealed class CommandTreeResponse
{
    public string SchemaVersion { get; init; } = OutputSchema.Version;

    public string Command { get; init; } = "tree";

    public required CommandNode Data { get; init; }
}
