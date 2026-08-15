using GdCli.Commands;
using GdCli.Contracts;
using GdCli.Database;
using GdCli.Features.Acquisition;
using GdCli.Features.Affixes;

namespace GdCli.Application;

internal sealed class ItemsCommand
{
    private readonly AffixDetailLoader _affixDetails;
    private readonly CliDatabase _database;

    public ItemsCommand(CliDatabase database)
    {
        _database = database;
        _affixDetails = new AffixDetailLoader(database);
    }

    public object Execute(CommandLineOptions options)
    {
        return options.GroupFamilies
            ? _queryFamilies(options)
            : _queryItems(options);
    }

    private QueryEnvelope<ItemFamily> _queryFamilies(CommandLineOptions options)
    {
        var filter = new ItemFamilyFilter(
            options.IsMi,
            options.Availability,
            options.AvailabilitySpecified && options.Availability == null);
        var total = _database.ItemFamilies.Count(filter);
        var page = _database.ItemFamilies.Load(
            filter,
            options.Offset,
            options.All ? null : options.Limit);
        return QueryEnvelopeFactory.Create(_database, options, "items", total, page);
    }

    private ItemQueryEnvelope _queryItems(CommandLineOptions options)
    {
        var includeUnavailable = options.ItemQuery != null ||
            options.AvailabilitySpecified && options.Availability == null;
        var filter = new ItemFilter(
            options.Rarity,
            options.ItemClass,
            options.MinimumLevel,
            options.MaximumLevel,
            options.IsMi,
            options.Availability,
            includeUnavailable,
            options.ItemQuery,
            options.ItemQuery != null);
        var total = _database.Items.Count(filter);
        if (options.ItemQuery != null && total == 0)
        {
            filter = filter with { ExactQuery = false };
            total = _database.Items.Count(filter);
        }

        var page = _database.Items.Load(
            filter,
            options.Offset,
            options.All ? null : options.Limit);
        _populateItems(page, !options.NoStats);

        IReadOnlyList<ItemSetRecord>? itemSets = null;
        if (options.ItemQuery != null)
        {
            _populateRelations(page, !options.NoStats);
            itemSets = _database.ItemSets.LoadForItems(page.Select(item => item.RecordId));
            _populateSets(itemSets, !options.NoStats);
        }

        return QueryEnvelopeFactory.CreateItems(_database, options, total, page, itemSets);
    }

    private void _populateItems(IReadOnlyList<ItemRecord> items, bool includeStats)
    {
        var miSources = _database.Acquisitions.LoadMiSources(
            items.Where(item => item.IsMi).Select(item => item.RecordId));
        var stats = includeStats
            ? _database.LoadStats(items.Select(item => item.RecordId))
            : null;
        foreach (var item in items)
        {
            if (stats != null)
                item.Stats = stats.GetValueOrDefault(item.RecordId) ?? [];
            var sources = miSources.GetValueOrDefault(item.RecordId);
            item.MiSources = sources is { Count: > 0 } ? sources : null;
        }
    }

    private void _populateRelations(List<ItemRecord> items, bool includeStats)
    {
        if (items.Count == 0)
            return;

        var variants = _database.ItemVariants.LoadForItems(items.Select(item => item.RecordId));
        if (includeStats)
        {
            _affixDetails.PopulateVariants(
                variants.Values.SelectMany(value => value).ToList());
        }

        var acquisitions = new AcquisitionResolver(_database.Acquisitions).Resolve(items);
        foreach (var item in items)
        {
            item.Variants = variants.GetValueOrDefault(item.RecordId) ?? [];
            item.Acquisition = acquisitions[item.RecordId];
        }
    }

    private void _populateSets(IReadOnlyList<ItemSetRecord> itemSets, bool includeStats)
    {
        if (!includeStats || itemSets.Count == 0)
            return;

        var stats = _database.LoadStats(itemSets.Select(itemSet => itemSet.RecordId));
        foreach (var itemSet in itemSets)
            itemSet.Stats = stats.GetValueOrDefault(itemSet.RecordId) ?? [];
    }
}
