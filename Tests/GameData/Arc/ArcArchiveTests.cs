using GdCli.GameData;
using GdCli.GameData.Arc;

namespace GdCli.Tests.GameData.Arc;

public sealed class ArcArchiveTests
{
    [Fact]
    public void ConstructorFailureReleasesTheArchiveFile()
    {
        var path = Path.Combine(Path.GetTempPath(), $"gd-cli-invalid-{Guid.NewGuid():N}.arc");
        File.WriteAllBytes(path, new byte[8]);

        try
        {
            Assert.Throws<GameDataException>(() => new ArcArchive(path));
            using var exclusive = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
