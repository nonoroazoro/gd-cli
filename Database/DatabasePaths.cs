namespace GdCli.Database;

internal static class DatabasePaths
{
    public static string Resolve()
    {
        return Path.Combine(AppContext.BaseDirectory, "data", "gd-cli.db");
    }

    public static string EnsureDirectory()
    {
        var path = Resolve();
        var directory = Path.GetDirectoryName(path)
            ?? throw new InvalidOperationException("CLI database directory could not be resolved.");
        Directory.CreateDirectory(directory);
        return path;
    }

    public static string ResolveExisting()
    {
        var path = Resolve();
        if (File.Exists(path))
            return path;

        throw new DatabaseNotFoundException($"CLI database was not found: {path}. Run init first.");
    }
}
