using System.Buffers.Binary;
using System.Text;

namespace GdCli.GameData.Map;

internal sealed class WorldMapReader : IDisposable
{
    private const int _scanBufferSize = 1024 * 1024;
    private const int _scanOverlap = 4096;
    private const int _levelTableSearchWindow = 256 * 1024;
    private readonly Stream _stream;
    private readonly BinaryReader _reader;
    private readonly IReadOnlyList<WorldMapLevel> _levels;
    private readonly bool _leaveOpen;

    public WorldMapReader(Stream stream, string source, bool leaveOpen = false)
    {
        Path = source;
        _stream = stream;
        _leaveOpen = leaveOpen;
        _reader = new BinaryReader(_stream, Encoding.UTF8, true);
        try
        {
            Span<byte> magic = stackalloc byte[4];
            _stream.ReadExactly(magic);
            if (!magic[..3].SequenceEqual("MAP"u8))
                throw new GameDataException($"Unsupported world map header in {source}");
            Version = magic[3];
            var firstPathOffset = _findFirstLevelPathOffset();
            _levels = _findAndReadLevelTable(firstPathOffset);
        }
        catch
        {
            _reader.Dispose();
            if (!_leaveOpen)
                _stream.Dispose();
            throw;
        }
    }

    public string Path { get; }

    public byte Version { get; }

    public IReadOnlyList<WorldMapLevel> Levels => _levels;

    public IEnumerable<PlacedObject> ReadPlacements(IReadOnlySet<string> relevantRecords)
    {
        foreach (var level in _levels)
        {
            foreach (var placement in _readLevelPlacements(level, relevantRecords))
                yield return placement;
        }
    }

    public void Dispose()
    {
        _reader.Dispose();
        if (!_leaveOpen)
            _stream.Dispose();
    }

    private List<WorldMapLevel> _findAndReadLevelTable(long firstPathOffset)
    {
        var start = Math.Max(4, firstPathOffset - _levelTableSearchWindow);
        _stream.Position = start;
        var window = new byte[checked((int)(firstPathOffset - start))];
        _stream.ReadExactly(window);
        for (var offset = window.Length - 60; offset >= 0; offset--)
        {
            var count = BinaryPrimitives.ReadInt32LittleEndian(window.AsSpan(offset, 4));
            if (count is <= 0 or > 10000)
                continue;
            var riftLength = BinaryPrimitives.ReadInt32LittleEndian(window.AsSpan(offset + 56, 4));
            if (riftLength is < 0 or > 4096 || offset + 60L + riftLength > window.Length)
                continue;
            if (riftLength > 0)
            {
                var rift = window.AsSpan(offset + 60, riftLength);
                if (!rift.StartsWith("records/"u8))
                    continue;
            }
            var tableOffset = start + offset;
            if (tableOffset >= firstPathOffset)
                continue;
            try
            {
                var levels = _readLevelTable(tableOffset, firstPathOffset);
                if (levels.Count > 0)
                    return levels;
            }
            catch (GameDataException)
            {
            }
            catch (EndOfStreamException)
            {
            }
        }
        throw new GameDataException($"World map level table was not found in {Path}");
    }

    private List<WorldMapLevel> _readLevelTable(long tableOffset, long expectedFirstPathOffset)
    {
        _stream.Position = tableOffset;
        var count = _reader.ReadInt32();
        if (count is <= 0 or > 10000)
            throw new GameDataException("World map level count is invalid.");

        var result = new List<WorldMapLevel>(count);
        for (var index = 0; index < count; index++)
        {
            for (var dimension = 0; dimension < 6; dimension++)
                _ = _reader.ReadInt32();
            var offsetX = _reader.ReadInt32();
            var offsetY = _reader.ReadInt32();
            var offsetZ = _reader.ReadInt32();
            _stream.Position += 16;
            var riftGateRecordId = _readLengthPrefixedString().Replace('\\', '/');

            string? levelPath = null;
            long levelPathOffset = 0;
            for (var field = 0; field < 256; field++)
            {
                var length = _reader.ReadInt32();
                if (length == 0)
                    continue;
                if (length is < 0 or > 4096)
                    throw new GameDataException("World map level metadata length is invalid.");
                levelPathOffset = _stream.Position;
                var value = Encoding.UTF8.GetString(_reader.ReadBytes(length)).Replace('\\', '/');
                if (value.EndsWith(".lvl", StringComparison.OrdinalIgnoreCase))
                {
                    levelPath = value;
                    break;
                }
            }
            if (levelPath == null)
                throw new GameDataException("World map level path was not found.");
            if (index == 0 && levelPathOffset != expectedFirstPathOffset)
                throw new GameDataException("World map level table candidate did not match the first level path.");

            var dataOffset = _reader.ReadUInt32();
            var dataLength = _reader.ReadInt32();
            if (dataLength <= 0 || dataOffset + (long)dataLength > _stream.Length)
                throw new GameDataException($"World map level data is outside the file: {levelPath}");
            result.Add(new WorldMapLevel
            {
                Path = levelPath,
                RiftGateRecordId = riftGateRecordId,
                OffsetX = offsetX,
                OffsetY = offsetY,
                OffsetZ = offsetZ,
                DataOffset = dataOffset,
                DataLength = dataLength
            });
        }
        return result;
    }

    private IEnumerable<PlacedObject> _readLevelPlacements(
        WorldMapLevel level,
        IReadOnlySet<string> relevantRecords)
    {
        _stream.Position = level.DataOffset;
        var magic = new byte[4];
        _stream.ReadExactly(magic);
        if (!magic.AsSpan(0, 3).SequenceEqual("LVL"u8))
            throw new GameDataException($"Invalid level header: {level.Path}");

        for (var index = 0; index < 6; index++)
            _ = _reader.ReadSingle();
        _ = _reader.ReadUInt32();
        _ = _reader.ReadUInt32();
        var stringCount = _reader.ReadInt32();
        if (stringCount is < 0 or > 1_000_000)
            throw new GameDataException($"Invalid level string count: {level.Path}");
        var records = new string[stringCount];
        for (var index = 0; index < stringCount; index++)
            records[index] = _readLengthPrefixedString().Replace('\\', '/');

        var entityCount = _reader.ReadInt32();
        if (entityCount is < 0 or > 10_000_000)
            throw new GameDataException($"Invalid level entity count: {level.Path}");
        for (var entityOrdinal = 0; entityOrdinal < entityCount; entityOrdinal++)
        {
            var recordIndex = _reader.ReadInt32();
            if (recordIndex < 0 || recordIndex >= records.Length)
                throw new GameDataException($"Invalid level record index: {level.Path}");
            var localX = 0f;
            var localY = 0f;
            var localZ = 0f;
            for (var index = 0; index < 12; index++)
            {
                var value = _reader.ReadSingle();
                if (index == 9)
                    localX = value;
                else if (index == 10)
                    localY = value;
                else if (index == 11)
                    localZ = value;
            }
            var linkCount = _reader.ReadInt32();
            if (linkCount is < 0 or > 1_000_000)
                throw new GameDataException($"Invalid level link count: {level.Path}");
            _stream.Position += linkCount * 16L;

            var recordId = records[recordIndex];
            if (!relevantRecords.Contains(recordId))
                continue;
            yield return new PlacedObject
            {
                LevelPath = level.Path,
                RiftGateRecordId = level.RiftGateRecordId,
                EntityOrdinal = entityOrdinal,
                RecordId = recordId,
                WorldX = level.OffsetX + localX,
                WorldY = level.OffsetY + localY,
                WorldZ = level.OffsetZ + localZ
            };
        }
    }

    private long _findFirstLevelPathOffset()
    {
        _stream.Position = 4;
        var buffer = new byte[_scanBufferSize + _scanOverlap];
        var carry = 0;
        var absoluteStart = 4L;
        while (true)
        {
            var read = _stream.Read(buffer, carry, _scanBufferSize);
            if (read == 0)
                break;
            var length = carry + read;
            var span = buffer.AsSpan(0, length);
            var searchOffset = 0;
            while (searchOffset < span.Length)
            {
                var relative = span[searchOffset..].IndexOf("Levels"u8);
                if (relative < 0)
                    break;
                relative += searchOffset;
                if (relative >= 4 && _tryGetLevelPathLength(span, relative, out _))
                    return absoluteStart + relative;
                searchOffset = relative + 1;
            }

            carry = Math.Min(_scanOverlap, length);
            span[(length - carry)..].CopyTo(buffer);
            absoluteStart += length - carry;
        }
        throw new GameDataException($"World map contains no level paths: {Path}");
    }

    private static bool _tryGetLevelPathLength(ReadOnlySpan<byte> data, int offset, out int length)
    {
        length = 0;
        var separatorOffset = offset + "Levels"u8.Length;
        if (separatorOffset >= data.Length ||
            (data[separatorOffset] != (byte)'/' && data[separatorOffset] != (byte)'\\'))
            return false;
        var declared = BinaryPrimitives.ReadUInt32LittleEndian(data[(offset - 4)..offset]);
        if (declared is < 11 or > 4096 || offset + declared > data.Length)
            return false;
        var value = data.Slice(offset, (int)declared);
        if (!value.EndsWith(".lvl"u8))
            return false;
        length = (int)declared;
        return true;
    }

    private string _readLengthPrefixedString()
    {
        var length = _reader.ReadInt32();
        if (length is < 0 or > 1_048_576)
            throw new GameDataException("Length-prefixed string is invalid.");
        return Encoding.UTF8.GetString(_reader.ReadBytes(length));
    }
}
