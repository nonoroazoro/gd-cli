using System.Globalization;

namespace GdCli.Database;

internal sealed class TemporaryDatabaseFile : IDisposable
{
    private bool _disposed;

    private TemporaryDatabaseFile(string path)
    {
        Path = path;
    }

    public string Path { get; }

    public static TemporaryDatabaseFile Create(string targetPath)
    {
        var directory = System.IO.Path.GetDirectoryName(targetPath)
            ?? throw new InvalidOperationException("CLI database directory could not be resolved.");
        var fileName = System.IO.Path.GetFileName(targetPath);
        for (var attempt = 0; attempt < 10; attempt++)
        {
            var path = System.IO.Path.Combine(
                directory,
                $"{fileName}.{Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture)}.tmp");
            try
            {
                using var file = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
                return new TemporaryDatabaseFile(path);
            }
            catch (IOException) when (File.Exists(path))
            {
            }
        }

        throw new IOException("A unique temporary CLI database could not be created.");
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        if (File.Exists(Path))
            File.Delete(Path);
    }
}
