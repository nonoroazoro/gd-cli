using GdCli.GameData.Binary;

namespace GdCli.Tests.GameData.Binary;

public sealed class GameBinaryReaderTests
{
    [Fact]
    public void ReadStringRejectsTruncatedPayload()
    {
        using var stream = new MemoryStream([4, 0, 0, 0, 65, 66]);
        using var reader = new GameBinaryReader(stream);

        Assert.Throws<EndOfStreamException>(reader.ReadString);
    }
}
