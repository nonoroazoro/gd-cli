using System.Buffers.Binary;
using GdCli.GameData.Arz;

namespace GdCli.Tests.GameData.Arz;

public sealed class ArzFieldReaderTests
{
    [Fact]
    public void ReadPreservesArrayPositionsWhenZeroValuesAreSkipped()
    {
        var data = new byte[24];
        BinaryPrimitives.WriteUInt16LittleEndian(data.AsSpan(0, 2), 0);
        BinaryPrimitives.WriteUInt16LittleEndian(data.AsSpan(2, 2), 4);
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(4, 4), 0);
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(8, 4), 0);
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(12, 4), 120);
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(16, 4), 0);
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(20, 4), 80);

        var fields = ArzFieldReader.Read(data, ["offensivePhysicalModifier"], "test.dbr");

        Assert.Equal([1, 3], fields.Select(field => field.Ordinal));
        Assert.Equal([120, 80], fields.Select(field => field.NumericValue));
    }
}
