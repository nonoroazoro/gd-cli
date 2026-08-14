using GdCli.Database;

namespace GdCli.Tests.Database;

public sealed class TemporaryDatabaseFileTests
{
    [Fact]
    public void DisposeDeletesOnlyTheCreatedTemporaryFile()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"gd-cli-database-file-{Guid.NewGuid():N}");
        var targetPath = Path.Combine(directory, "gd-cli.db");
        var unrelatedPath = Path.Combine(directory, "gd-cli.db.unrelated.tmp");
        Directory.CreateDirectory(directory);
        File.WriteAllText(targetPath, "target");
        File.WriteAllText(unrelatedPath, "unrelated");

        try
        {
            string temporaryPath;
            using (var temporaryDatabase = TemporaryDatabaseFile.Create(targetPath))
            {
                temporaryPath = temporaryDatabase.Path;
                Assert.True(File.Exists(temporaryPath));
            }

            Assert.False(File.Exists(temporaryPath));
            Assert.Equal("target", File.ReadAllText(targetPath));
            Assert.Equal("unrelated", File.ReadAllText(unrelatedPath));
        }
        finally
        {
            if (Directory.Exists(directory))
                Directory.Delete(directory, true);
        }
    }
}
