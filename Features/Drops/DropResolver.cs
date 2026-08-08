using GdCli.Contracts;
using GdCli.Database;

namespace GdCli.Features.Drops;

internal sealed class DropResolver
{
    private const int _maximumDepth = 8;
    private const int _maximumRoutes = 512;
    private readonly CliDatabase _database;
    private readonly Dictionary<string, IReadOnlyList<DropCondition>> _conditionCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, IReadOnlyList<DropLocation>> _locationCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, IReadOnlyList<DropReference>> _referenceCache = new(StringComparer.OrdinalIgnoreCase);

    public DropResolver(CliDatabase database)
    {
        _database = database;
    }

    public IReadOnlyList<ItemDropResult> Resolve(IReadOnlyList<ItemRecord> items)
    {
        var miSources = _database.LoadMiSources(items.Select(item => item.RecordId));
        return items.Select(item => _resolve(item, miSources.GetValueOrDefault(item.RecordId) ?? [])).ToList();
    }

    private ItemDropResult _resolve(ItemRecord item, IReadOnlyList<MonsterSource> miSources)
    {
        var routes = new List<DropRoute>();
        var routeKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var queue = new Queue<DropSearchState>();
        var depthLimitReached = false;
        var routeLimitReached = false;
        queue.Enqueue(new DropSearchState(item.RecordId, [], new HashSet<string>(StringComparer.OrdinalIgnoreCase) { item.RecordId }));
        while (queue.Count > 0 && !routeLimitReached)
        {
            var state = queue.Dequeue();
            var references = _loadReferences(state.RecordId);
            if (state.Path.Count >= _maximumDepth)
            {
                depthLimitReached |= references.Any(reference => !state.Visited.Contains(reference.SourceRecordId));
                continue;
            }
            foreach (var reference in references)
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
                    Conditions = _loadConditions(reference.SourceRecordId, reference.Field)
                }).ToList();
                foreach (var location in _loadLocations(reference.SourceRecordId))
                {
                    var route = new DropRoute { Path = path, Location = location };
                    if (!routeKeys.Add(_routeKey(route)))
                        continue;
                    if (routes.Count == _maximumRoutes)
                    {
                        routeLimitReached = true;
                        break;
                    }
                    routes.Add(route);
                }
                if (routeLimitReached)
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
            NameTag = item.NameTag,
            Rarity = item.Rarity,
            IsMi = item.IsMi,
            MiSources = miSources,
            Routes = routes,
            RoutesTruncated = routeLimitReached || depthLimitReached,
            RouteLimit = _maximumRoutes,
            MaximumDepth = _maximumDepth
        };
    }

    private IReadOnlyList<DropReference> _loadReferences(string recordId)
    {
        if (!_referenceCache.TryGetValue(recordId, out var references))
        {
            references = _database.LoadReverseDropReferences(recordId);
            _referenceCache[recordId] = references;
        }
        return references;
    }

    private List<DropCondition> _loadConditions(string recordId, string field)
    {
        if (!_conditionCache.TryGetValue(recordId, out var conditions))
        {
            conditions = _database.LoadDropConditions(recordId);
            _conditionCache[recordId] = conditions;
        }
        return conditions.Where(condition => DropConditionMatcher.IsMatch(field, condition.Field)).ToList();
    }

    private IReadOnlyList<DropLocation> _loadLocations(string recordId)
    {
        if (!_locationCache.TryGetValue(recordId, out var locations))
        {
            locations = _database.LoadLocations(recordId);
            _locationCache[recordId] = locations;
        }
        return locations;
    }

    private static string _routeKey(DropRoute route)
    {
        return $"{route.Location.Source}|{route.Location.Level}|{route.Location.X:R}|{route.Location.Y:R}|{route.Location.Z:R}|{string.Join('>', route.Path.Select(step => step.RecordId))}";
    }
}
