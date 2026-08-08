namespace GdCli.Commands;

internal sealed class CommandNode
{
    public required string Name { get; init; }

    public required string Kind { get; init; }

    public required string Description { get; init; }

    public IReadOnlyList<string> Arguments { get; init; } = [];

    public IReadOnlyList<string> Options { get; init; } = [];

    public IReadOnlyList<CommandNode> Children { get; init; } = [];
}
