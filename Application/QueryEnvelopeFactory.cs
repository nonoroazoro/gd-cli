using GdCli.Commands;
using GdCli.Contracts;
using GdCli.Database;

namespace GdCli.Application;

internal static class QueryEnvelopeFactory
{
    public static QueryEnvelope<T> Create<T>(
        CliDatabase database,
        CommandLineOptions options,
        string command,
        int total,
        IReadOnlyList<T> data)
    {
        var page = _page(options, total, data.Count);
        return new QueryEnvelope<T>
        {
            Command = command,
            Database = database.Path,
            Count = data.Count,
            Total = total,
            Offset = options.Offset,
            Limit = page.Limit,
            HasMore = page.HasMore,
            NextOffset = page.NextOffset,
            Data = data
        };
    }

    public static ItemQueryEnvelope CreateItems(
        CliDatabase database,
        CommandLineOptions options,
        int total,
        IReadOnlyList<ItemRecord> data,
        IReadOnlyList<ItemSetRecord>? itemSets)
    {
        var page = _page(options, total, data.Count);
        return new ItemQueryEnvelope
        {
            Command = "items",
            Database = database.Path,
            Count = data.Count,
            Total = total,
            Offset = options.Offset,
            Limit = page.Limit,
            HasMore = page.HasMore,
            NextOffset = page.NextOffset,
            Data = data,
            ItemSets = itemSets
        };
    }

    private static (int? Limit, bool HasMore, int? NextOffset) _page(
        CommandLineOptions options,
        int total,
        int count)
    {
        var endOffset = (long)options.Offset + count;
        var hasMore = endOffset < total;
        return (
            options.All ? null : options.Limit,
            hasMore,
            hasMore ? (int)endOffset : null);
    }
}
