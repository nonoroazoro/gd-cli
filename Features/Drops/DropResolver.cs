using GdCli.Contracts;
using GdCli.Database;

namespace GdCli.Features.Drops;

internal sealed class DropResolver
{
    private const int _maximumDepth = 8;
    private const int _maximumRoutes = 512;
    private readonly CliDatabase _database;

    public DropResolver(CliDatabase database)
    {
        _database = database;
    }

    public ItemDropResult Resolve(ItemRecord item)
    {
        var miSources = _database.LoadMiSources([item.RecordId]).GetValueOrDefault(item.RecordId) ?? [];
        var routes = new List<DropRoute>();
        var queue = new Queue<DropSearchState>();
        queue.Enqueue(new DropSearchState(item.RecordId, [], new HashSet<string>(StringComparer.OrdinalIgnoreCase) { item.RecordId }));
        while (queue.Count > 0 && routes.Count < _maximumRoutes)
        {
            var state = queue.Dequeue();
            if (state.Path.Count >= _maximumDepth)
                continue;
            foreach (var reference in _database.LoadReverseDropReferences(state.RecordId))
            {
                if (state.Visited.Contains(reference.SourceRecordId))
                    continue;
                var visited = new HashSet<string>(state.Visited, StringComparer.OrdinalIgnoreCase)
                {
                    reference.SourceRecordId
                };
                var path = state.Path.Append(new DropPathStep
                {
                    RecordId = reference.SourceRecordId,
                    Name = reference.SourceName,
                    RecordClass = reference.SourceClass,
                    Field = reference.Field,
                    Conditions = _database.LoadDropConditions(reference.SourceRecordId, reference.Field)
                }).ToList();
                foreach (var location in _database.LoadLocations(reference.SourceRecordId))
                {
                    routes.Add(new DropRoute { Path = path, Location = location });
                    if (routes.Count >= _maximumRoutes)
                        break;
                }
                if (routes.Count >= _maximumRoutes)
                    break;
                queue.Enqueue(new DropSearchState(
                    reference.SourceRecordId,
                    path,
                    visited));
            }
        }

        return new ItemDropResult
        {
            RecordId = item.RecordId,
            Name = item.Name,
            Rarity = item.Rarity,
            IsMi = item.IsMi,
            MiSources = miSources,
            Routes = routes
                .DistinctBy(_routeKey, StringComparer.OrdinalIgnoreCase)
                .ToList()
        };
    }

    private static string _routeKey(DropRoute route)
    {
        return $"{route.Location.Source}|{route.Location.Level}|{route.Location.X:R}|{route.Location.Y:R}|{route.Location.Z:R}|{string.Join('>', route.Path.Select(step => step.RecordId))}";
    }
}
