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
                if (options.KindSpecified)
                    throw new CommandLineException("--kind is not valid for items.");
                if (options.AscendedCategorySpecified)
                    throw new CommandLineException("--category is not valid for items.");
                break;
            case "item-families":
                _rejectGameLanguage(options);
                _rejectNoStats(options);
                if (options.RaritySpecified ||
                    options.ItemClassSpecified ||
                    options.KindSpecified ||
                    options.AscendedCategorySpecified ||
                    options.MinimumLevel.HasValue ||
                    options.MaximumLevel.HasValue)
                    throw new CommandLineException("Only --mi is a valid filter for item-families.");
                break;
            case "item":
            case "affix":
            case "ascended-affix":
                _rejectFilters(options);
                _rejectPaging(options);
                _rejectGameLanguage(options);
                break;
            case "affixes":
                _rejectGameLanguage(options);
                if (options.AscendedCategorySpecified)
                    throw new CommandLineException("--category is not valid for affixes.");
                if (options.MiSpecified)
                    throw new CommandLineException("--mi is not valid for affixes.");
                break;
            case "ascended-affixes":
                _rejectGameLanguage(options);
                if (options.RaritySpecified ||
                    options.ItemClassSpecified ||
                    options.KindSpecified ||
                    options.MiSpecified ||
                    options.MinimumLevel.HasValue ||
                    options.MaximumLevel.HasValue)
                    throw new CommandLineException(
                        "Only --category is a valid filter for ascended-affixes.");
                break;
            case "search":
                _rejectGameLanguage(options);
                _rejectNoStats(options);
                if (options.ItemClass != null && options.Kind != null)
                    throw new CommandLineException("--type and --kind cannot be combined for search.");
                if (options.MiSpecified)
                    throw new CommandLineException("--mi is not valid for search.");
                if (options.AscendedCategorySpecified)
                    throw new CommandLineException("--category is not valid for search.");
                break;
            case "drops":
            case "quest":
                _rejectFilters(options);
                _rejectGameLanguage(options);
                _rejectNoStats(options);
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
            options.AscendedCategorySpecified ||
            options.MiSpecified ||
            options.MinimumLevel.HasValue ||
            options.MaximumLevel.HasValue)
            throw new CommandLineException($"Filters are not valid for {options.Command}.");
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
