using GdCli.Commands;
using GdCli.Contracts;
using GdCli.Database;
using GdCli.Features.Affixes;

namespace GdCli.Application;

internal sealed class AffixesCommand
{
    private readonly AffixDetailLoader _details;
    private readonly CliDatabase _database;

    public AffixesCommand(CliDatabase database)
    {
        _database = database;
        _details = new AffixDetailLoader(database);
    }

    public QueryEnvelope<AffixRecord> Execute(CommandLineOptions options)
    {
        var filter = new AffixFilter(
            options.AffixFamily,
            options.Rarity,
            options.Kind,
            options.ItemClass,
            options.AscendedCategory,
            options.MinimumLevel,
            options.MaximumLevel,
            options.AffixQuery,
            options.AffixQuery != null);
        var total = _database.Affixes.Count(filter);
        if (options.AffixQuery != null && total == 0)
        {
            filter = filter with { ExactQuery = false };
            total = _database.Affixes.Count(filter);
        }

        var page = _database.Affixes.Load(
            filter,
            options.Offset,
            options.All ? null : options.Limit);
        if (!options.NoStats)
            _details.Populate(page);
        return QueryEnvelopeFactory.Create(_database, options, "affixes", total, page);
    }
}
