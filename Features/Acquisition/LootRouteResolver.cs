using GdCli.Contracts;
using GdCli.Database;

namespace GdCli.Features.Acquisition;

internal sealed class LootRouteResolver
{
    private const int _maximumDepth = 8;
    private const int _maximumRoutes = 512;
    private readonly Dictionary<string, IReadOnlyList<LootCondition>> _conditionCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, IReadOnlyList<AcquisitionLocation>> _locationCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, IReadOnlyList<LootReference>> _referenceCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly AcquisitionRepository _repository;

    public LootRouteResolver(AcquisitionRepository repository)
    {
        _repository = repository;
    }

    public LootRouteResult Resolve(string itemRecordId)
    {
        var routes = new List<AcquisitionRoute>();
        var routeKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var queue = new Queue<LootSearchState>();
        var depthLimitReached = false;
        var routeLimitReached = false;
        queue.Enqueue(new LootSearchState(
            itemRecordId,
            [],
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { itemRecordId }));
        while (queue.Count > 0 && !routeLimitReached)
        {
            var state = queue.Dequeue();
            var references = _loadReferences(state.RecordId);
            if (state.Path.Count >= _maximumDepth)
            {
                depthLimitReached |= references.Any(reference =>
                    !state.Visited.Contains(reference.SourceRecordId));
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
                var path = state.Path.Append(new LootPathStep
                {
                    RecordId = reference.SourceRecordId,
                    Name = reference.SourceName,
                    RecordClass = reference.SourceClass,
                    Field = reference.Field,
                    Conditions = _loadConditions(reference.SourceRecordId, reference.Field)
                }).ToList();
                foreach (var location in _loadLocations(reference.SourceRecordId))
                {
                    var route = new AcquisitionRoute { Path = path, Location = location };
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
                queue.Enqueue(new LootSearchState(reference.SourceRecordId, path, visited));
            }
        }

        return new LootRouteResult
        {
            Routes = routes,
            RoutesTruncated = routeLimitReached || depthLimitReached,
            RouteLimit = _maximumRoutes,
            MaximumDepth = _maximumDepth
        };
    }

    private List<LootCondition> _loadConditions(string recordId, string field)
    {
        if (!_conditionCache.TryGetValue(recordId, out var conditions))
        {
            conditions = _repository.LoadLootConditions(recordId);
            _conditionCache[recordId] = conditions;
        }
        return conditions.Where(condition => LootConditionMatcher.IsMatch(field, condition.Field)).ToList();
    }

    private IReadOnlyList<AcquisitionLocation> _loadLocations(string recordId)
    {
        if (!_locationCache.TryGetValue(recordId, out var locations))
        {
            locations = _repository.LoadLocations(recordId);
            _locationCache[recordId] = locations;
        }
        return locations;
    }

    private IReadOnlyList<LootReference> _loadReferences(string recordId)
    {
        if (!_referenceCache.TryGetValue(recordId, out var references))
        {
            references = _repository.LoadReverseLootReferences(recordId);
            _referenceCache[recordId] = references;
        }
        return references;
    }

    private static string _routeKey(AcquisitionRoute route)
    {
        return $"{route.Location.Source}|{route.Location.Level}|{route.Location.X:R}|" +
               $"{route.Location.Y:R}|{route.Location.Z:R}|" +
               string.Join('>', route.Path.Select(step => $"{step.RecordId}|{step.Field}"));
    }
}
