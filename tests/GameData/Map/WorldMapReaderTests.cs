using GdCli.GameData;
using GdCli.GameData.Map;

namespace GdCli.Tests.GameData.Map;

public sealed class WorldMapReaderTests
{
    [Fact]
    public void ConstructorFailureReleasesTheOwnedStream()
    {
        var path = Path.Combine(Path.GetTempPath(), $"gd-cli-invalid-{Guid.NewGuid():N}.map");
        File.WriteAllBytes(path, "BAD0"u8.ToArray());

        try
        {
            var stream = GameFile.OpenRead(path);
            Assert.Throws<GameDataException>(() => new WorldMapReader(stream, path));
            using var exclusive = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
