using GdCli.GameData;

namespace GdCli.Tests.GameData;

public sealed class GameInstallTests
{
    [Fact]
    public void OpenOrdersExpansionSourcesForLastWriteWins()
    {
        var root = Path.Combine(Path.GetTempPath(), "gd-cli-tests", Guid.NewGuid().ToString("N"));
        try
        {
            _createSource(root, "base");
            _createSource(root, "gdx2");
            _createSource(root, "gdx1");

            var install = GameInstall.Open(root, "en");

            Assert.Equal(["base", "gdx1", "gdx2"], install.Sources.Select(source => source.Name));
            Assert.Equal([0, 1, 2], install.Sources.Select(source => source.Priority));
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, true);
        }
    }

    private static void _createSource(string root, string name)
    {
        var source = name == "base" ? root : Path.Combine(root, name);
        var database = Path.Combine(source, "database");
        var resources = Path.Combine(source, "resources");
        Directory.CreateDirectory(database);
        Directory.CreateDirectory(resources);
        File.WriteAllBytes(Path.Combine(database, $"{name}.arz"), []);
        File.WriteAllBytes(Path.Combine(resources, "Text_EN.arc"), []);
    }
}
