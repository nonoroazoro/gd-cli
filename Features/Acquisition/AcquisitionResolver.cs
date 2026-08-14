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

    public IReadOnlyList<AcquisitionResult> Resolve(IReadOnlyList<ItemRecord> items)
    {
        var recipes = _repository.LoadRecipes(items.Select(item => item.RecordId));
        var sourceItemRecords = items
            .Select(item => item.RecordId)
            .Concat(recipes.Values.SelectMany(value => value).Select(item => item.RecordId));
        var sources = _repository.LoadSources(sourceItemRecords);
        return items.Select(item => new AcquisitionResult
        {
            Item = _item(item),
            Methods = _methods(
                item.RecordId,
                sources.GetValueOrDefault(item.RecordId) ?? [],
                recipes.GetValueOrDefault(item.RecordId) ?? [],
                sources)
        }).ToList();
    }

    private AcquisitionActor _actor(IGrouping<string, AcquisitionSourceRecord> sources)
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
        return new AcquisitionActor
        {
            RecordIds = records,
            Name = _actorName(sources.First(), records),
            Locations = locations
        };
    }

    private List<AcquisitionMethod> _directMethods(
        string itemRecordId,
        IReadOnlyList<AcquisitionSourceRecord> sources)
    {
        var methods = new List<AcquisitionMethod>();
        var vendors = _actors(sources, AcquisitionKind.Vendor);
        if (vendors.Count > 0)
        {
            methods.Add(new AcquisitionMethod
            {
                Kind = AcquisitionKind.Vendor,
                Actors = vendors
            });
        }

        var monsters = _actors(sources, AcquisitionKind.SpecificMonster);
        if (monsters.Count > 0)
        {
            var routeResult = _routeResolver.Resolve(itemRecordId);
            methods.Add(new AcquisitionMethod
            {
                Kind = AcquisitionKind.SpecificMonster,
                Actors = monsters,
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

    private static AcquisitionItem _item(ItemRecord item)
    {
        return new AcquisitionItem
        {
            RecordId = item.RecordId,
            Name = item.Name,
            NameTag = item.NameTag,
            Rarity = item.Rarity,
            ItemClass = item.ItemClass
        };
    }

    private List<AcquisitionActor> _actors(
        IReadOnlyList<AcquisitionSourceRecord> sources,
        string kind)
    {
        return sources
            .Where(source => source.Kind == kind && source.RecordId != null)
            .GroupBy(_actorKey, StringComparer.OrdinalIgnoreCase)
            .Select(_actor)
            .OrderBy(actor => actor.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private IReadOnlyList<AcquisitionLocation> _loadLocations(string recordId)
    {
        if (!_locationCache.TryGetValue(recordId, out var locations))
        {
            locations = _repository.LoadActorLocations(recordId);
            _locationCache[recordId] = locations;
        }
        return locations;
    }

    private static string _locationKey(AcquisitionLocation location)
    {
        return $"{location.Source}|{location.Level}|{location.PlacedRecordId}|" +
               $"{location.X:R}|{location.Y:R}|{location.Z:R}";
    }

    private static string _actorKey(AcquisitionSourceRecord source)
    {
        if (!string.IsNullOrWhiteSpace(source.NameTag))
            return $"tag:{source.NameTag}";
        return $"record:{source.RecordId}";
    }

    private static string _actorName(
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
        IReadOnlyList<AcquisitionItem> recipes,
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
