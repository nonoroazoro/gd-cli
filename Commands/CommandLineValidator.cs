namespace GdCli.Commands;

internal static class CommandLineValidator
{
    public static void Validate(CommandLineOptions options)
    {
        switch (options.Command)
        {
            case "tree":
                _rejectFilters(options);
                _rejectPaging(options);
                _rejectGameLanguage(options);
                _rejectNoStats(options);
                break;
            case "init":
                _rejectFilters(options);
                _rejectPaging(options);
                _rejectNoStats(options);
                break;
            case "info":
            case "schema":
                _rejectFilters(options);
                _rejectPaging(options);
                _rejectGameLanguage(options);
                _rejectNoStats(options);
                break;
            case "items":
                _rejectGameLanguage(options);
                if (options.KindSpecified || options.AffixFamilySpecified || options.AscendedCategorySpecified)
                    throw new CommandLineException("Affix filters are not valid for items.");
                if (options.GroupFamilies && options.ItemQuery != null)
                    throw new CommandLineException("--families cannot be combined with an item query.");
                if (options.GroupFamilies && (options.RaritySpecified || options.ItemClassSpecified ||
                    options.MinimumLevel.HasValue || options.MaximumLevel.HasValue || options.NoStats))
                    throw new CommandLineException(
                        "Only --mi, --availability, and paging are valid with --families.");
                break;
            case "affixes":
                _rejectGameLanguage(options);
                if (options.GroupFamilies)
                    throw new CommandLineException("--families is not valid for affixes.");
                if (options.MiSpecified)
                    throw new CommandLineException("--mi is not valid for affixes.");
                if (options.AvailabilitySpecified)
                    throw new CommandLineException("--availability is not valid for affixes.");
                if (options.AffixFamily?.Equals("standard", StringComparison.OrdinalIgnoreCase) == true &&
                    options.AscendedCategorySpecified)
                {
                    throw new CommandLineException(
                        "--category is not valid with --family standard.");
                }
                if (options.AffixFamily?.Equals("ascended", StringComparison.OrdinalIgnoreCase) == true &&
                    (options.ItemClassSpecified || options.KindSpecified))
                {
                    throw new CommandLineException(
                        "--type and --kind are not valid with --family ascended.");
                }
                if (options.KindSpecified && options.AscendedCategorySpecified)
                {
                    throw new CommandLineException(
                        "--kind and --category target different affix families.");
                }
                break;
            case "quests":
                _rejectFilters(options);
                _rejectGameLanguage(options);
                _rejectNoStats(options);
                break;
        }
    }

    private static void _rejectFilters(CommandLineOptions options)
    {
        if (options.RaritySpecified ||
            options.ItemClassSpecified ||
            options.KindSpecified ||
            options.AffixFamilySpecified ||
            options.AscendedCategorySpecified ||
            options.MiSpecified ||
            options.AvailabilitySpecified ||
            options.MinimumLevel.HasValue ||
            options.MaximumLevel.HasValue)
            throw new CommandLineException($"Filters are not valid for {options.Command}.");
        if (options.GroupFamilies)
            throw new CommandLineException($"--families is not valid for {options.Command}.");
    }

    private static void _rejectPaging(CommandLineOptions options)
    {
        if (options.OffsetSpecified || options.LimitSpecified || options.All)
            throw new CommandLineException($"Paging options are not valid for {options.Command}.");
    }

    private static void _rejectGameLanguage(CommandLineOptions options)
    {
        if (options.GameLanguageSpecified)
            throw new CommandLineException($"--game-language is not valid for {options.Command}.");
    }

    private static void _rejectNoStats(CommandLineOptions options)
    {
        if (options.NoStats)
            throw new CommandLineException($"--no-stats is not valid for {options.Command}.");
    }
}
