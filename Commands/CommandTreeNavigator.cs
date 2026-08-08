namespace GdCli.Commands;

internal static class CommandTreeNavigator
{
    public static CommandNode? FindChild(CommandNode parent, string name)
    {
        return parent.Children.FirstOrDefault(child =>
            child.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    public static CommandNode Resolve(CommandNode root, IReadOnlyList<string> path)
    {
        var current = root;
        foreach (var segment in path)
        {
            current = FindChild(current, segment)
                ?? throw new CommandLineException($"Unknown command: {string.Join(' ', path)}");
        }

        return current;
    }
}
