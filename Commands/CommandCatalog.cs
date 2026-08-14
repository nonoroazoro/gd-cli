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
                "Query item records.",
                options:
                [
                    "--rarity VALUE|all",
                    "--type VALUE|all",
                    "--min-level N",
                    "--max-level N",
                    "--mi true|false",
                    "--offset N",
                    "--limit N",
                    "--all",
                    "--no-stats"
                ]),
            _command(
                "item-families",
                "Query item records grouped by stable game name tag.",
                options:
                [
                    "--mi true|false",
                    "--offset N",
                    "--limit N",
                    "--all"
                ]),
            _command("item", "Get one item by exact record ID.", ["record-id"], ["--no-stats"]),
            _command(
                "affixes",
                "Query prefix and suffix records.",
                options:
                [
                    "--rarity VALUE|all",
                    "--kind prefix|suffix|all",
                    "--type VALUE|all",
                    "--min-level N",
                    "--max-level N",
                    "--offset N",
                    "--limit N",
                    "--all",
                    "--no-stats"
                ]),
            _command("affix", "Get one affix by exact record ID.", ["record-id"], ["--no-stats"]),
            _command(
                "ascended-affixes",
                "Query Ascended affixes by game-native equipment category.",
                options:
                [
                    "--category VALUE|all",
                    "--offset N",
                    "--limit N",
                    "--all",
                    "--no-stats"
                ]),
            _command(
                "ascended-affix",
                "Get one Ascended affix by exact record ID.",
                ["record-id"],
                ["--no-stats"]),
            _command(
                "drops",
                "Find monster-specific item drops and map locations.",
                ["item-name-or-record-id"],
                ["--offset N", "--limit N", "--all"]),
            _command(
                "quests",
                "Query quests.",
                options: ["--offset N", "--limit N", "--all"]),
            _command(
                "quest",
                "Get quest graph, actors, and key coordinates.",
                ["quest-name-or-path"],
                ["--offset N", "--limit N", "--all"]),
            _command(
                "search",
                "Search item and affix names or record IDs.",
                ["query"],
                [
                    "--rarity VALUE|all",
                    "--type VALUE|all",
                    "--kind prefix|suffix|all",
                    "--min-level N",
                    "--max-level N",
                    "--offset N",
                    "--limit N",
                    "--all"
                ])
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
