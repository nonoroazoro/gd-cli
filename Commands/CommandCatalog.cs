namespace GdCli.Commands;

internal static class CommandCatalog
{
    private static readonly string[] _globalOptions = ["--query JMESPATH"];
    private static readonly string[] _helpOptions = ["--help", "-h"];

    private static readonly CommandNode _root = new()
    {
        Name = "gd-cli",
        Kind = "root",
        Description = "Build and query Grim Dawn game data.",
        Options = [.. _globalOptions, .. _helpOptions],
        Children =
        [
            _command("tree", "Show the command tree."),
            _command(
                "init",
                "Rebuild the CLI database from game data.",
                ["grim-dawn-game-directory"],
                ["--game-language en|zh (default: zh)"]),
            _command("info", "Show database metadata and available values."),
            _command("schema", "Show fields, capabilities, and valid filter values."),
            _command(
                "items",
                "Query items and related set, variant, and acquisition data.",
                ["query (optional)"],
                options:
                [
                    "--rarity VALUE|all",
                    "--type VALUE|all",
                    "--min-level N",
                    "--max-level N",
                    "--mi true|false",
                    "--availability known|referenced|unresolved|unavailable|all",
                    "--families",
                    "--offset N",
                    "--limit N",
                    "--all",
                    "--no-stats"
                ]),
            _command(
                "affixes",
                "Query standard and Ascended affixes.",
                ["query (optional)"],
                options:
                [
                    "--family standard|ascended|all",
                    "--rarity VALUE|all",
                    "--kind prefix|suffix|all",
                    "--type VALUE|all",
                    "--category VALUE|all",
                    "--min-level N",
                    "--max-level N",
                    "--offset N",
                    "--limit N",
                    "--all",
                    "--no-stats"
                ]),
            _command(
                "quests",
                "Query quest summaries or a detailed quest graph.",
                ["query (optional)"],
                options: ["--offset N", "--limit N", "--all"]),
        ]
    };

    public static IReadOnlySet<string> CommandNames { get; } = _root.Children
        .Select(command => command.Name)
        .ToHashSet(StringComparer.OrdinalIgnoreCase);

    public static IReadOnlyList<string> GlobalFlags { get; } = _globalOptions;

    public static IReadOnlyList<string> HelpFlags { get; } = _helpOptions;

    public static CommandNode GetTree()
    {
        return _root;
    }

    public static CommandNode GetCommand(IReadOnlyList<string> path)
    {
        return CommandTreeNavigator.Resolve(_root, path);
    }

    private static CommandNode _command(
        string name,
        string description,
        IReadOnlyList<string>? arguments = null,
        IReadOnlyList<string>? options = null)
    {
        return new CommandNode
        {
            Name = name,
            Kind = "command",
            Description = description,
            Arguments = arguments ?? [],
            Options = [.. (options ?? []), .. _helpOptions]
        };
    }
}
