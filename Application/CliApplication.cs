using GdCli.Commands;
using GdCli.Contracts;
using GdCli.Database;
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
                JsonOutput.Write(
                    _standardOutput,
                    new ItemsCommand(database).Execute(options),
                    options.OutputQuery);
                return (int)ExitCode.Success;
            case "affixes":
                JsonOutput.Write(
                    _standardOutput,
                    new AffixesCommand(database).Execute(options),
                    options.OutputQuery);
                return (int)ExitCode.Success;
            case "quests":
                JsonOutput.Write(
                    _standardOutput,
                    new QuestsCommand(database).Execute(options),
                    options.OutputQuery);
                return (int)ExitCode.Success;
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
            AffixFamilies = info.AffixFamilies,
            AffixKinds = info.AffixKinds,
            AscendedCategories = info.AscendedCategories,
            Availabilities = info.Availabilities
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
        if (options.AffixFamily != null)
            _validateFilter("--family", options.AffixFamily, database.GetAffixFamilies());
        if (options.Kind != null)
            _validateFilter("--kind", options.Kind, database.GetAffixKinds());
        if (options.AscendedCategory != null)
        {
            _validateFilter(
                "--category",
                options.AscendedCategory,
                database.GetAscendedCategories());
        }
        if (options.Availability != null)
            _validateFilter("--availability", options.Availability, database.GetAvailabilities());
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
            _ => "unexpected_error"
        };
    }
}
