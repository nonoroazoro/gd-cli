using System.Text;

namespace GdCli.GameData.Binary;

internal sealed class GameBinaryReader : IDisposable
{
    private const uint _maximumStringLength = 16 * 1024 * 1024;
    private readonly BinaryReader _reader;
    private readonly Stream _stream;
    private readonly bool _leaveOpen;

    public GameBinaryReader(Stream stream, bool leaveOpen = false)
    {
        _stream = stream;
        _reader = new BinaryReader(_stream, Encoding.UTF8, true);
        _leaveOpen = leaveOpen;
    }

    public long Position => _stream.Position;

    public bool ReadMagic(ReadOnlySpan<byte> expected)
    {
        Span<byte> actual = stackalloc byte[expected.Length];
        _stream.ReadExactly(actual);
        return actual.SequenceEqual(expected);
    }

    public byte ReadByte() => _reader.ReadByte();

    public bool ReadBoolean() => _reader.ReadByte() != 0;

    public int ReadInt32() => _reader.ReadInt32();

    public uint ReadUInt32() => _reader.ReadUInt32();

    public float ReadSingle() => _reader.ReadSingle();

    public string ReadString()
    {
        var length = ReadUInt32();
        if (length == 0)
            return string.Empty;
        if (length > _maximumStringLength)
            throw new GameDataException($"Game-data string is too large: {length} bytes.");
        return Encoding.UTF8.GetString(_readBytes(checked((int)length)));
    }

    public string ReadUnicodeString()
    {
        var length = ReadUInt32();
        if (length == 0)
            return string.Empty;
        if (length > _maximumStringLength / 2)
            throw new GameDataException($"Game-data Unicode string is too large: {length} characters.");
        return Encoding.Unicode.GetString(_readBytes(checked((int)length * 2)));
    }

    public void Dispose()
    {
        _reader.Dispose();
        if (!_leaveOpen)
            _stream.Dispose();
    }

    private byte[] _readBytes(int length)
    {
        var result = GC.AllocateUninitializedArray<byte>(length);
        _stream.ReadExactly(result);
        return result;
    }
}
