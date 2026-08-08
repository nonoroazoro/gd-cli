namespace GdCli.GameData;

internal sealed class GameInstall
{
    private GameInstall(string root, string gameLanguage, IReadOnlyList<GameDataSource> sources)
    {
        Root = root;
        GameLanguage = gameLanguage;
        Sources = sources;
    }

    public string Root { get; }

    public string GameLanguage { get; }

    public IReadOnlyList<GameDataSource> Sources { get; }

    public static GameInstall Open(string path, string gameLanguage)
    {
        var root = Path.GetFullPath(path);
        if (!Directory.Exists(root))
            throw new GameDataException($"Grim Dawn directory was not found: {root}");

        var normalizedLanguage = gameLanguage.ToUpperInvariant();
        if (normalizedLanguage is not ("EN" or "ZH"))
            throw new GameDataException("Game-data language must be en or zh.");

        var roots = new List<(string Name, string Path)> { ("base", root) };
        roots.AddRange(Directory.GetDirectories(root, "gdx*", SearchOption.TopDirectoryOnly)
            .Select(directory => (Name: Path.GetFileName(directory).ToLowerInvariant(), Path: directory))
            .OrderBy(entry => entry.Name, StringComparer.OrdinalIgnoreCase));

        var sources = new List<GameDataSource>();
        for (var priority = 0; priority < roots.Count; priority++)
        {
            var entry = roots[priority];
            var databaseDirectory = Path.Combine(entry.Path, "database");
            var arzPath = Directory.Exists(databaseDirectory)
                ? Directory.GetFiles(databaseDirectory, "*.arz", SearchOption.TopDirectoryOnly).SingleOrDefault()
                : null;
            if (arzPath == null)
            {
                if (priority == 0)
                    throw new GameDataException($"Base database ARZ was not found under: {databaseDirectory}");
                continue;
            }

            var resourcesDirectory = Path.Combine(entry.Path, "resources");
            var englishTags = _findFile(resourcesDirectory, "Text_EN.arc");
            var localizedTags = normalizedLanguage == "EN"
                ? englishTags
                : _findFile(resourcesDirectory, $"Text_{normalizedLanguage}.arc");
            var levels = _findFile(resourcesDirectory, "Levels.arc");
            sources.Add(new GameDataSource
            {
                Name = entry.Name,
                Priority = priority,
                Root = entry.Path,
                ArzPath = arzPath,
                EnglishTagsPath = englishTags,
                LocalizedTagsPath = localizedTags,
                LevelsPath = levels
            });
        }

        if (sources.Count == 0)
            throw new GameDataException("No Grim Dawn game data sources were found.");
        if (sources[0].EnglishTagsPath == null)
            throw new GameDataException("Base English text archive was not found.");
        if (normalizedLanguage != "EN" && sources.All(source => source.LocalizedTagsPath == null))
            throw new GameDataException($"Text_{normalizedLanguage}.arc was not found.");

        return new GameInstall(root, normalizedLanguage, sources);
    }

    private static string? _findFile(string directory, string name)
    {
        if (!Directory.Exists(directory))
            return null;
        return Directory.GetFiles(directory, "*.arc", SearchOption.TopDirectoryOnly)
            .FirstOrDefault(path => Path.GetFileName(path).Equals(name, StringComparison.OrdinalIgnoreCase));
    }
}
