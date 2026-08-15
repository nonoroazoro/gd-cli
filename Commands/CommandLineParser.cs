using System.Globalization;

namespace GdCli.Commands;

internal static class CommandLineParser
{
    public static CommandLineOptions Parse(string[] args)
    {
        return Parse(args, CommandCatalog.GetTree());
    }

    public static CommandLineOptions Parse(string[] args, CommandNode root)
    {
        var options = new CommandLineOptions();
        var positionals = new List<string>();
        var commandPath = new List<string>();
        var commandNode = root;

        for (var index = 0; index < args.Length; index++)
        {
            var value = args[index];
            if (value is "--help" or "-h")
            {
                options.HelpRequested = true;
                continue;
            }

            if (!value.StartsWith('-'))
            {
                var child = positionals.Count == 0
                    ? CommandTreeNavigator.FindChild(commandNode, value)
                    : null;
                if (child != null)
                {
                    commandNode = child;
                    commandPath.Add(child.Name);
                    options.Command = child.Name;
                }
                else if (commandPath.Count == 0)
                {
                    throw new CommandLineException($"Unknown command: {value}");
                }
                else if (commandNode.Children.Count > 0 && commandNode.Arguments.Count == 0)
                {
                    throw new CommandLineException(
                        $"Unknown command: {string.Join(' ', commandPath.Append(value))}");
                }
                else
                {
                    positionals.Add(value);
                }

                continue;
            }

            switch (value)
            {
                case "--query":
                    options.OutputQuery = _next(args, ref index, value);
                    break;
                case "--all":
                    options.All = true;
                    break;
                case "--rarity":
                    options.Rarity = _normalizeAll(_next(args, ref index, value));
                    options.RaritySpecified = true;
                    break;
                case "--type":
                    options.ItemClass = _normalizeAll(_next(args, ref index, value));
                    options.ItemClassSpecified = true;
                    break;
                case "--kind":
                    options.Kind = _normalizeAll(_next(args, ref index, value));
                    options.KindSpecified = true;
                    break;
                case "--family":
                    options.AffixFamily = _normalizeAll(_next(args, ref index, value));
                    options.AffixFamilySpecified = true;
                    break;
                case "--category":
                    options.AscendedCategory = _normalizeAll(_next(args, ref index, value));
                    options.AscendedCategorySpecified = true;
                    break;
                case "--mi":
                    options.IsMi = _parseBoolean(_next(args, ref index, value), value);
                    options.MiSpecified = true;
                    break;
                case "--availability":
                    options.Availability = _normalizeAll(_next(args, ref index, value));
                    options.AvailabilitySpecified = true;
                    break;
                case "--min-level":
                    options.MinimumLevel = _parseNonNegative(_next(args, ref index, value), value);
                    break;
                case "--max-level":
                    options.MaximumLevel = _parseNonNegative(_next(args, ref index, value), value);
                    break;
                case "--offset":
                    options.Offset = _parseNonNegative(_next(args, ref index, value), value);
                    options.OffsetSpecified = true;
                    break;
                case "--limit":
                    options.Limit = _parsePositive(_next(args, ref index, value), value);
                    options.LimitSpecified = true;
                    break;
                case "--game-language":
                    options.GameLanguage = _next(args, ref index, value).ToLowerInvariant();
                    options.GameLanguageSpecified = true;
                    if (options.GameLanguage is not ("en" or "zh"))
                        throw new CommandLineException("--game-language must be en or zh.");
                    break;
                case "--no-stats":
                    options.NoStats = true;
                    break;
                case "--families":
                    options.GroupFamilies = true;
                    break;
                default:
                    throw new CommandLineException($"Unknown option: {value}");
            }
        }

        if (options.MinimumLevel > options.MaximumLevel)
            throw new CommandLineException("--min-level cannot be greater than --max-level.");
        if (options.All && (options.OffsetSpecified || options.LimitSpecified))
            throw new CommandLineException("--all cannot be combined with --offset or --limit.");
        if (options.OutputQuery != null && string.IsNullOrWhiteSpace(options.OutputQuery))
            throw new CommandLineException("--query requires a non-empty JMESPath expression.");
        if (options.RaritySpecified && options.Rarity != null && string.IsNullOrWhiteSpace(options.Rarity))
            throw new CommandLineException("--rarity requires a non-empty value.");
        if (options.ItemClassSpecified && options.ItemClass != null && string.IsNullOrWhiteSpace(options.ItemClass))
            throw new CommandLineException("--type requires a non-empty value.");
        if (options.KindSpecified && options.Kind != null && string.IsNullOrWhiteSpace(options.Kind))
            throw new CommandLineException("--kind requires a non-empty value.");
        if (options.AffixFamilySpecified &&
            options.AffixFamily != null &&
            string.IsNullOrWhiteSpace(options.AffixFamily))
            throw new CommandLineException("--family requires a non-empty value.");
        if (options.AscendedCategorySpecified &&
            options.AscendedCategory != null &&
            string.IsNullOrWhiteSpace(options.AscendedCategory))
            throw new CommandLineException("--category requires a non-empty value.");
        if (options.AvailabilitySpecified &&
            options.Availability != null &&
            string.IsNullOrWhiteSpace(options.Availability))
            throw new CommandLineException("--availability requires a non-empty value.");
        options.CommandPath = commandPath;
        if (options.HelpRequested)
            return options;
        if (commandPath.Count == 0)
            throw new CommandLineException("A command is required. Use --help to list commands.");
        _applyPositionals(options, positionals);
        return options;
    }

    private static void _applyPositionals(CommandLineOptions options, List<string> positionals)
    {
        switch (options.Command)
        {
            case "init":
                if (positionals.Count != 1)
                    throw new CommandLineException("init requires exactly one Grim Dawn game directory.");
                options.GameDirectory = positionals[0];
                break;
            case "items":
                options.ItemQuery = positionals.Count == 0 ? null : string.Join(' ', positionals);
                break;
            case "affixes":
                options.AffixQuery = positionals.Count == 0 ? null : string.Join(' ', positionals);
                break;
            case "quests":
                options.QuestQuery = positionals.Count == 0 ? null : string.Join(' ', positionals);
                break;
            default:
                if (positionals.Count > 0)
                    throw new CommandLineException($"{options.Command} does not accept positional arguments.");
                break;
        }
    }

    private static string _next(string[] args, ref int index, string option)
    {
        index++;
        if (index >= args.Length || args[index].StartsWith('-'))
            throw new CommandLineException($"Missing value for {option}.");
        return args[index];
    }

    private static string? _normalizeAll(string value)
    {
        return value.Equals("all", StringComparison.OrdinalIgnoreCase) ? null : value;
    }

    private static int _parseNonNegative(string value, string option)
    {
        if (!int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var result) || result < 0)
            throw new CommandLineException($"{option} requires a non-negative integer.");
        return result;
    }

    private static int _parsePositive(string value, string option)
    {
        var result = _parseNonNegative(value, option);
        if (result == 0)
            throw new CommandLineException($"{option} requires an integer greater than zero.");
        return result;
    }

    private static bool _parseBoolean(string value, string option)
    {
        if (bool.TryParse(value, out var result))
            return result;
        throw new CommandLineException($"{option} must be true or false.");
    }
}
