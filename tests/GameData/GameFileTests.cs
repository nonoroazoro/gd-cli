using GdCli.GameData;

namespace GdCli.Tests.GameData;

public sealed class GameFileTests
{
    [Fact]
    public void OpenReadAllowsWriteRenameAndDelete()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"gd-cli-game-file-{Guid.NewGuid():N}");
        var path = Path.Combine(directory, "game-data.arc");
        var movedPath = Path.Combine(directory, "renamed.arc");
        Directory.CreateDirectory(directory);
        File.WriteAllBytes(path, [1, 2, 3]);

        try
        {
            using var reader = GameFile.OpenRead(path);
            using (var writer = new FileStream(
                path,
                FileMode.Open,
                FileAccess.Write,
                FileShare.ReadWrite | FileShare.Delete))
            {
                writer.WriteByte(4);
            }

            File.Move(path, movedPath);
            File.Delete(movedPath);
            Assert.False(File.Exists(movedPath));
        }
        finally
        {
            if (Directory.Exists(directory))
                Directory.Delete(directory, true);
        }
    }
}
