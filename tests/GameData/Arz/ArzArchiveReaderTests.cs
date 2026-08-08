using GdCli.GameData;
using GdCli.GameData.Arz;

namespace GdCli.Tests.GameData.Arz;

public sealed class ArzArchiveReaderTests
{
    [Fact]
    public void ConstructorFailureReleasesTheArchiveFile()
    {
        var path = Path.Combine(Path.GetTempPath(), $"gd-cli-invalid-{Guid.NewGuid():N}.arz");
        File.WriteAllBytes(path, new byte[4]);

        try
        {
            Assert.Throws<GameDataException>(() => new ArzArchiveReader(path));
            using var exclusive = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
