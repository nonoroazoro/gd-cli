using GdCli.Commands;
using GdCli.Contracts;
using GdCli.Database;
using GdCli.Features.Affixes;
using GdCli.Features.Affixes.Formatting;
using GdCli.Features.Drops;
using GdCli.Output;

namespace GdCli.Application;

internal sealed class CliApplication
{
    private readonly TextWriter _standardOutput;
    private readonly TextWriter _standardError;

    public CliApplication(TextWriter standardOutput, TextWriter standardError)
    {
        _standardOutput = standardOutput;
        _standardError = standardError;
    }

    public int Run(string[] args)
    {
        CommandLineOptions options;
        try
        {
            options = CommandLineParser.Parse(args);
            if (!options.HelpRequested)
                CommandLineValidator.Validate(options);
            JsonOutput.ValidateQuery(options.OutputQuery);
        }
        catch (CommandLineException exception)
        {
            return _writeError(ExitCode.InvalidArguments, exception.Message);
        }
        catch (OutputQueryException exception)
        {
            return _writeError(ExitCode.InvalidArguments, exception.Message);
        }

        if (options.HelpRequested || options.Command == "tree")
        {
            try
            {
                object output;
                if (options.HelpRequested && options.CommandPath.Count > 0)
                {
                    var command = CommandCatalog.GetCommand(options.CommandPath);
                    output = new CommandHelpResponse
                    {
                        Command = string.Join(' ', options.CommandPath),
                        Description = command.Description,
                        Arguments = command.Arguments,
                        Options = command.Options,
                        Commands = command.Children.Count == 0
                            ? null
                            : command.Children.Select(child => new CommandSummary
                            {
                                Name = child.Name,
                                Description = child.Description
                            }).ToList()
                    };
                }
                else if (options.Command == "tree")
                {
                    output = new CommandTreeResponse { Data = CommandCatalog.GetTree() };
                }
                else
                {
                    output = new HelpResponse();
                }
                JsonOutput.Write(_standardOutput, output, options.OutputQuery);
                return (int)ExitCode.Success;
            }
            catch (OutputQueryException exception)
            {
                return _writeError(ExitCode.InvalidArguments, exception.Message);
            }
            catch (JsonSerializationOutputException exception)
            {
                return _writeError(ExitCode.UnexpectedError, exception.Message);
            }
        }

        if (options.Command == "init")
        {
            try
            {
                var result = DatabaseInitializer.Initialize(
                    options.GameDirectory ?? string.Empty,
                    options.GameLanguage);
                JsonOutput.Write(_standardOutput, result, options.OutputQuery);
                return (int)ExitCode.Success;
            }
            catch (OutputQueryException exception)
            {
                return _writeError(ExitCode.InvalidArguments, exception.Message);
            }
            catch (Exception exception)
            {
                return _writeError(ExitCode.UnexpectedError, exception.Message);
            }
        }

        try
        {
            var databasePath = DatabasePaths.ResolveExisting();
            using var database = new CliDatabase(databasePath);
            _validateFilterValues(database, options);
            return _execute(database, options);
        }
        catch (DatabaseNotFoundException exception)
        {
            return _writeError(ExitCode.DatabaseNotFound, exception.Message);
        }
        catch (IncompatibleDatabaseException exception)
        {
            return _writeError(ExitCode.IncompatibleDatabase, exception.Message);
        }
        catch (OutputQueryException exception)
        {
            return _writeError(ExitCode.InvalidArguments, exception.Message);
        }
        catch (InvalidFilterValueException exception)
        {
            return _writeFilterError(exception);
        }
        catch (JsonSerializationOutputException exception)
        {
            return _writeError(ExitCode.UnexpectedError, exception.Message);
        }
        catch (Exception exception)
        {
            return _writeError(ExitCode.UnexpectedError, exception.Message);
        }
    }

    private int _execute(CliDatabase database, CommandLineOptions options)
    {
        switch (options.Command)
        {
            case "info":
                JsonOutput.Write(_standardOutput, database.GetInfo(), options.OutputQuery);
                return (int)ExitCode.Success;
            case "schema":
                _writeSchema(database, options);
                return (int)ExitCode.Success;
            case "items":
                _writeItems(database, options);
                return (int)ExitCode.Success;
            case "item-families":
                _writeItemFamilies(database, options);
                return (int)ExitCode.Success;
            case "item":
                return _writeItem(database, options);
            case "affixes":
                _writeAffixes(database, options);
                return (int)ExitCode.Success;
            case "affix":
                return _writeAffix(database, options);
            case "ascended-affixes":
                _writeAscendedAffixes(database, options);
                return (int)ExitCode.Success;
            case "ascended-affix":
                return _writeAscendedAffix(database, options);
            case "search":
                _writeSearch(database, options);
                return (int)ExitCode.Success;
            case "drops":
                return _writeDrops(database, options);
            default:
                return _writeError(ExitCode.InvalidArguments, $"Unknown command: {options.Command}");
        }
    }

    private void _writeSchema(CliDatabase database, CommandLineOptions options)
    {
        var info = database.GetInfo();
        JsonOutput.Write(_standardOutput, new SchemaDescription
        {
            Database = database.Path,
            Rarities = info.Rarities,
            ItemClasses = info.ItemClasses,
            AffixKinds = info.AffixKinds,
            AscendedCategories = info.AscendedCategories
        }, options.OutputQuery);
    }

    private void _writeItems(CliDatabase database, CommandLineOptions options)
    {
        var filter = new ItemFilter(
            options.Rarity,
            options.ItemClass,
            options.MinimumLevel,
            options.MaximumLevel,
            options.IsMi);
        var total = database.Items.Count(filter);
        var page = database.Items.Load(filter, options.Offset, options.All ? null : options.Limit);
        _populateItemDetails(database, page, !options.NoStats);
        _writeEnvelope(database, options, "items", total, page);
    }

    private void _writeItemFamilies(CliDatabase database, CommandLineOptions options)
    {
        var filter = new ItemFamilyFilter(options.IsMi);
        var total = database.ItemFamilies.Count(filter);
        var page = database.ItemFamilies.Load(filter, options.Offset, options.All ? null : options.Limit);
        _writeEnvelope(database, options, "item-families", total, page);
    }

    private int _writeItem(CliDatabase database, CommandLineOptions options)
    {
        var item = database.Items.FindByRecordId(options.RecordId ?? string.Empty);
        if (item == null)
            return _writeError(ExitCode.RecordNotFound, $"Item record was not found: {options.RecordId}");

        _populateItemDetails(database, [item], !options.NoStats);
        _writeEnvelope(database, options, "item", 1, [item], 0, 1);
        return (int)ExitCode.Success;
    }

    private void _writeAffixes(CliDatabase database, CommandLineOptions options)
    {
        var filter = new AffixFilter(
            options.Rarity,
            options.Kind,
            options.ItemClass,
            options.MinimumLevel,
            options.MaximumLevel);
        var total = database.Affixes.Count(filter);
        var page = database.Affixes.Load(filter, options.Offset, options.All ? null : options.Limit);
        if (!options.NoStats)
            _populateAffixDetails(database, page);
        _writeEnvelope(database, options, "affixes", total, page);
    }

    private int _writeAffix(CliDatabase database, CommandLineOptions options)
    {
        var affix = database.Affixes.FindByRecordId(options.RecordId ?? string.Empty);
        if (affix == null)
            return _writeError(ExitCode.RecordNotFound, $"Affix record was not found: {options.RecordId}");

        if (!options.NoStats)
            _populateAffixDetails(database, [affix]);
        _writeEnvelope(database, options, "affix", 1, [affix], 0, 1);
        return (int)ExitCode.Success;
    }

    private void _writeAscendedAffixes(CliDatabase database, CommandLineOptions options)
    {
        var filter = new AscendedAffixFilter(options.AscendedCategory);
        var total = database.AscendedAffixes.Count(filter);
        var page = database.AscendedAffixes.Load(
            filter,
            options.Offset,
            options.All ? null : options.Limit);
        if (!options.NoStats)
            _populateAscendedAffixDetails(database, page);
        _writeEnvelope(database, options, "ascended-affixes", total, page);
    }

    private int _writeAscendedAffix(CliDatabase database, CommandLineOptions options)
    {
        var affix = database.AscendedAffixes.FindByRecordId(options.RecordId ?? string.Empty);
        if (affix == null)
        {
            return _writeError(
                ExitCode.RecordNotFound,
                $"Ascended affix record was not found: {options.RecordId}");
        }

        if (!options.NoStats)
            _populateAscendedAffixDetails(database, [affix]);
        _writeEnvelope(database, options, "ascended-affix", 1, [affix], 0, 1);
        return (int)ExitCode.Success;
    }

    private void _writeSearch(CliDatabase database, CommandLineOptions options)
    {
        var filter = new SearchFilter(
            options.SearchQuery ?? string.Empty,
            options.Rarity,
            options.ItemClass,
            options.Kind,
            options.MinimumLevel,
            options.MaximumLevel);
        var total = database.Search.Count(filter);
        var page = database.Search.Load(filter, options.Offset, options.All ? null : options.Limit);
        _writeEnvelope(database, options, "search", total, page);
    }

    private int _writeDrops(CliDatabase database, CommandLineOptions options)
    {
        var query = options.DropQuery ?? string.Empty;
        var exact = database.Items.CountMatches(query, true, false) > 0;
        var matchCount = database.Items.CountMatches(query, exact, false);
        if (matchCount == 0)
            return _writeError(ExitCode.RecordNotFound, $"Item was not found: {query}");

        var total = database.Items.CountMatches(query, exact, true);
        if (total == 0)
            return _writeError(ExitCode.NotMi, $"Item has no monster-specific drop data: {query}");

        var page = database.Items.LoadMatches(query, exact, true, options.Offset, options.All ? null : options.Limit);
        var resolver = new DropResolver(database);
        var data = resolver.Resolve(page);
        _writeEnvelope(database, options, "drops", total, data);
        return (int)ExitCode.Success;
    }

    private static void _populateItemDetails(
        CliDatabase database,
        IReadOnlyList<ItemRecord> items,
        bool includeStats)
    {
        if (!includeStats)
            return;

        var stats = database.LoadStats(items.Select(item => item.RecordId));
        var miSources = database.LoadMiSources(items.Select(item => item.RecordId));
        foreach (var item in items)
        {
            item.Stats = stats.GetValueOrDefault(item.RecordId) ?? [];
            item.MiSources = miSources.GetValueOrDefault(item.RecordId) ?? [];
        }
    }

    private static void _populateAffixDetails(CliDatabase database, List<AffixRecord> affixes)
    {
        if (affixes.Count == 0)
            return;

        var stats = database.LoadStats(affixes.Select(affix => affix.RecordId));
        var statTags = new EnglishStatTags(database.LoadTags());
        var effectBuilder = new AffixEffectBuilder(statTags);
        foreach (var affix in affixes)
        {
            affix.Stats = stats.GetValueOrDefault(affix.RecordId) ?? [];
            effectBuilder.Apply(affix);
        }
    }

    private static void _populateAscendedAffixDetails(
        CliDatabase database,
        List<AscendedAffixRecord> affixes)
    {
        if (affixes.Count == 0)
            return;

        var stats = database.LoadStats(affixes.Select(affix => affix.RecordId));
        var modifiers = database.AscendedAffixes.LoadSkillModifiers(
            affixes.Select(affix => affix.RecordId));
        var modifierStats = database.LoadStats(
            modifiers.Values.SelectMany(value => value).Select(modifier => modifier.RecordId));
        var statTags = new EnglishStatTags(database.LoadTags());
        var effectBuilder = new AffixEffectBuilder(statTags);
        foreach (var affix in affixes)
        {
            affix.Stats = stats.GetValueOrDefault(affix.RecordId) ?? [];
            affix.SkillModifiers = modifiers.GetValueOrDefault(affix.RecordId) ?? [];
            foreach (var modifier in affix.SkillModifiers)
                modifier.Stats = modifierStats.GetValueOrDefault(modifier.RecordId) ?? [];
            effectBuilder.Apply(affix);
        }
    }

    private void _writeEnvelope<T>(
        CliDatabase database,
        CommandLineOptions options,
        string command,
        int total,
        IReadOnlyList<T> data,
        int? offset = null,
        int? limit = null)
    {
        var actualOffset = offset ?? options.Offset;
        var actualLimit = limit ?? (options.All ? null : options.Limit);
        var endOffset = (long)actualOffset + data.Count;
        var hasMore = endOffset < total;
        JsonOutput.Write(_standardOutput, new QueryEnvelope<T>
        {
            Command = command,
            Database = database.Path,
            Count = data.Count,
            Total = total,
            Offset = actualOffset,
            Limit = actualLimit,
            HasMore = hasMore,
            NextOffset = hasMore ? (int)endOffset : null,
            Data = data
        }, options.OutputQuery);
    }

    private int _writeError(ExitCode exitCode, string message)
    {
        try
        {
            JsonOutput.Write(_standardError, new ErrorResponse
            {
                Code = _getErrorCode(exitCode),
                Error = message,
                ExitCode = (int)exitCode
            });
        }
        catch (JsonSerializationOutputException exception)
        {
            _standardError.WriteLine($"Output error: {exception.Message}");
            return (int)exitCode;
        }

        return (int)exitCode;
    }

    private int _writeFilterError(InvalidFilterValueException exception)
    {
        try
        {
            JsonOutput.Write(_standardError, new ErrorResponse
            {
                Code = "invalid_filter_value",
                Error = exception.Message,
                ExitCode = (int)ExitCode.InvalidArguments,
                Argument = exception.Argument,
                Value = exception.Value,
                AllowedValues = exception.AllowedValues
            });
        }
        catch (JsonSerializationOutputException outputException)
        {
            _standardError.WriteLine($"Output error: {outputException.Message}");
        }

        return (int)ExitCode.InvalidArguments;
    }

    private static void _validateFilterValues(CliDatabase database, CommandLineOptions options)
    {
        if (options.Rarity != null)
            _validateFilter("--rarity", options.Rarity, database.GetRarities());
        if (options.ItemClass != null)
            _validateFilter("--type", options.ItemClass, database.GetItemClasses());
        if (options.Kind != null)
            _validateFilter("--kind", options.Kind, database.GetAffixKinds());
        if (options.AscendedCategory != null)
        {
            _validateFilter(
                "--category",
                options.AscendedCategory,
                database.GetAscendedCategories());
        }
    }

    private static void _validateFilter(string argument, string? value, IReadOnlyList<string> allowedValues)
    {
        if (value != null && !allowedValues.Contains(value, StringComparer.OrdinalIgnoreCase))
            throw new InvalidFilterValueException(argument, value, allowedValues);
    }

    private static string _getErrorCode(ExitCode exitCode)
    {
        return exitCode switch
        {
            ExitCode.InvalidArguments => "invalid_arguments",
            ExitCode.DatabaseNotFound => "database_not_found",
            ExitCode.IncompatibleDatabase => "incompatible_database",
            ExitCode.RecordNotFound => "record_not_found",
            ExitCode.NotMi => "not_mi",
            _ => "unexpected_error"
        };
    }
}
