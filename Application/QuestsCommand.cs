using GdCli.Commands;
using GdCli.Contracts;
using GdCli.Database;

namespace GdCli.Application;

internal sealed class QuestsCommand
{
    private readonly CliDatabase _database;

    public QuestsCommand(CliDatabase database)
    {
        _database = database;
    }

    public QueryEnvelope<QuestRecord> Execute(CommandLineOptions options)
    {
        var query = options.QuestQuery;
        if (query == null)
        {
            var total = _database.Quests.Count();
            var page = _database.Quests.Load(
                options.Offset,
                options.All ? null : options.Limit);
            return QueryEnvelopeFactory.Create(_database, options, "quests", total, page);
        }

        var exactTotal = _database.Quests.CountMatches(query, true);
        var exact = exactTotal > 0;
        var matchTotal = exact ? exactTotal : _database.Quests.CountMatches(query, false);
        var matches = _database.Quests.LoadMatches(
            query,
            exact,
            options.Offset,
            options.All ? null : options.Limit);
        _database.Quests.PopulateDetails(matches);
        return QueryEnvelopeFactory.Create(_database, options, "quests", matchTotal, matches);
    }
}
