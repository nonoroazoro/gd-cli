using GdCli.GameData;
using GdCli.GameData.Arc;

namespace GdCli.Tests.GameData.Arc;

public sealed class ArcEntryStreamTests
{
    [Fact]
    public void ConstructorFailureReleasesTheArchiveFile()
    {
        var path = Path.Combine(Path.GetTempPath(), $"gd-cli-invalid-{Guid.NewGuid():N}.arc");
        File.WriteAllBytes(path, [1]);
        var parts = new ArcPart[]
        {
            new()
            {
                Offset = 0,
                CompressedLength = 1,
                DecompressedLength = 1
            }
        };

        try
        {
            Assert.Throws<GameDataException>(() => new ArcEntryStream(path, parts, 2));
            using var exclusive = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
