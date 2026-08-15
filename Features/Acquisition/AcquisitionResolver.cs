using GdCli.Contracts;
using GdCli.Database;

namespace GdCli.Features.Acquisition;

internal sealed class AcquisitionResolver
{
    private readonly Dictionary<string, IReadOnlyList<AcquisitionLocation>> _locationCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly LootRouteResolver _routeResolver;
    private readonly AcquisitionRepository _repository;

    public AcquisitionResolver(AcquisitionRepository repository)
    {
        _repository = repository;
        _routeResolver = new LootRouteResolver(repository);
    }

    public IReadOnlyDictionary<string, IReadOnlyList<AcquisitionMethod>> Resolve(
        IReadOnlyList<ItemRecord> items)
    {
        var recipes = _repository.LoadRecipes(items.Select(item => item.RecordId));
        var sourceItemRecords = items
            .Select(item => item.RecordId)
            .Concat(recipes.Values.SelectMany(value => value).Select(item => item.RecordId));
        var sources = _repository.LoadSources(sourceItemRecords);
        return items.ToDictionary(
            item => item.RecordId,
            item => (IReadOnlyList<AcquisitionMethod>)_methods(
                item.RecordId,
                sources.GetValueOrDefault(item.RecordId) ?? [],
                recipes.GetValueOrDefault(item.RecordId) ?? [],
                sources),
            StringComparer.OrdinalIgnoreCase);
    }

    private AcquisitionEntity _entity(IGrouping<string, AcquisitionSourceRecord> sources)
    {
        var records = sources
            .Select(source => source.RecordId ?? string.Empty)
            .Where(recordId => recordId.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var locations = records
            .SelectMany(_loadLocations)
            .GroupBy(_locationKey, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToList();
        return new AcquisitionEntity
        {
            RecordIds = records,
            Name = _entityName(sources.First(), records),
            Locations = locations
        };
    }

    private List<AcquisitionMethod> _directMethods(
        string itemRecordId,
        IReadOnlyList<AcquisitionSourceRecord> sources)
    {
        var methods = new List<AcquisitionMethod>();
        var vendors = _entities(sources, AcquisitionKind.Vendor);
        if (vendors.Count > 0)
        {
            methods.Add(new AcquisitionMethod
            {
                Kind = AcquisitionKind.Vendor,
                Entities = vendors
            });
        }

        var monsters = _entities(sources, AcquisitionKind.SpecificMonster);
        if (monsters.Count > 0)
        {
            var routeResult = _routeResolver.Resolve(
                itemRecordId,
                monsters.SelectMany(monster => monster.RecordIds));
            methods.Add(new AcquisitionMethod
            {
                Kind = AcquisitionKind.SpecificMonster,
                Entities = monsters,
                Routes = routeResult.Routes,
                RoutesTruncated = routeResult.RoutesTruncated,
                RouteLimit = routeResult.RouteLimit,
                MaximumDepth = routeResult.MaximumDepth
            });
        }

        var containers = _entities(sources, AcquisitionKind.Container);
        if (containers.Count > 0)
        {
            var routeResult = _routeResolver.Resolve(
                itemRecordId,
                containers.SelectMany(container => container.RecordIds));
            methods.Add(new AcquisitionMethod
            {
                Kind = AcquisitionKind.Container,
                Entities = containers,
                Routes = routeResult.Routes,
                RoutesTruncated = routeResult.RoutesTruncated,
                RouteLimit = routeResult.RouteLimit,
                MaximumDepth = routeResult.MaximumDepth
            });
        }

        if (sources.Any(source => source.Kind == AcquisitionKind.RandomDrop))
            methods.Add(new AcquisitionMethod { Kind = AcquisitionKind.RandomDrop });
        return methods;
    }

    private List<AcquisitionEntity> _entities(
        IReadOnlyList<AcquisitionSourceRecord> sources,
        string kind)
    {
        return sources
            .Where(source => source.Kind == kind && source.RecordId != null)
            .GroupBy(_entityKey, StringComparer.OrdinalIgnoreCase)
            .Select(_entity)
            .OrderBy(entity => entity.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private IReadOnlyList<AcquisitionLocation> _loadLocations(string recordId)
    {
        if (!_locationCache.TryGetValue(recordId, out var locations))
        {
            locations = _repository.LoadEntityLocations(recordId);
            _locationCache[recordId] = locations;
        }
        return locations;
    }

    private static string _locationKey(AcquisitionLocation location)
    {
        return $"{location.Source}|{location.Level}|{location.PlacedRecordId}|" +
               $"{location.X:R}|{location.Y:R}|{location.Z:R}";
    }

    private static string _entityKey(AcquisitionSourceRecord source)
    {
        if (!string.IsNullOrWhiteSpace(source.NameTag))
            return $"tag:{source.NameTag}";
        return $"record:{source.RecordId}";
    }

    private static string _entityName(
        AcquisitionSourceRecord source,
        List<string> recordIds)
    {
        return !string.IsNullOrWhiteSpace(source.Name)
            ? source.Name
            : recordIds.Count > 0 ? recordIds[0] : string.Empty;
    }

    private List<AcquisitionMethod> _methods(
        string itemRecordId,
        IReadOnlyList<AcquisitionSourceRecord> directSources,
        IReadOnlyList<ItemSummary> recipes,
        IReadOnlyDictionary<string, List<AcquisitionSourceRecord>> allSources)
    {
        var methods = _directMethods(itemRecordId, directSources);
        foreach (var recipe in recipes)
        {
            var recipeSources = _directMethods(
                recipe.RecordId,
                allSources.GetValueOrDefault(recipe.RecordId) ?? []);
            methods.Add(new AcquisitionMethod
            {
                Kind = AcquisitionKind.Craft,
                Recipe = recipe,
                Sources = recipeSources.Count == 0 ? null : recipeSources
            });
        }
        if (methods.Count == 0)
            methods.Add(new AcquisitionMethod { Kind = AcquisitionKind.Unknown });
        return methods;
    }
}
