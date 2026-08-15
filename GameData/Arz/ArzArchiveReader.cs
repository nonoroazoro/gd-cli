using System.Text;

namespace GdCli.GameData.Arz;

internal sealed class ArzArchiveReader : IDisposable
{
    private readonly FileStream _stream;
    private readonly List<string> _strings;
    private readonly List<ArzRecordHeader> _records;

    public ArzArchiveReader(string path)
    {
        Path = path;
        _stream = GameFile.OpenRead(path);
        try
        {
            using var reader = new BinaryReader(_stream, Encoding.UTF8, true);
            var unknown = reader.ReadUInt16();
            var version = reader.ReadUInt16();
            if (unknown != 2 || version != 3)
                throw new GameDataException($"Unsupported ARZ header in {path}: {unknown}/{version}");

            var recordTableStart = reader.ReadUInt32();
            _ = reader.ReadUInt32();
            var recordCount = reader.ReadUInt32();
            var stringTableStart = reader.ReadUInt32();
            var stringTableSize = reader.ReadUInt32();
            _strings = _readStringTable(reader, stringTableStart, stringTableSize);
            _records = _readRecordHeaders(reader, recordTableStart, recordCount);
        }
        catch
        {
            _stream.Dispose();
            throw;
        }
    }

    public string Path { get; }

    public int Count => _records.Count;

    public IEnumerable<ArzRecord> ReadRecords()
    {
        foreach (var header in _records)
            yield return _readRecord(header);
    }

    public void Dispose()
    {
        _stream.Dispose();
    }

    private ArzRecord _readRecord(ArzRecordHeader header)
    {
        if (header.RecordNameIndex >= _strings.Count)
            throw new GameDataException($"Invalid ARZ record name index: {header.RecordNameIndex}");
        if (header.CompressedSize < 0 || header.UncompressedSize < 0)
            throw new GameDataException($"Invalid ARZ record size: {_strings[(int)header.RecordNameIndex]}");
        var dataOffset = header.Offset + 24L;
        if (dataOffset > _stream.Length || header.CompressedSize > _stream.Length - dataOffset)
            throw new GameDataException($"ARZ record data is outside the archive: {_strings[(int)header.RecordNameIndex]}");
        _stream.Position = dataOffset;
        var compressed = new byte[header.CompressedSize];
        _stream.ReadExactly(compressed);
        var data = LZ4.LZ4Codec.Decode(compressed, 0, compressed.Length, header.UncompressedSize);
        var recordId = _strings[(int)header.RecordNameIndex];

        return new ArzRecord
        {
            RecordId = recordId.Replace('\\', '/'),
            Fields = ArzFieldReader.Read(data, _strings, recordId)
        };
    }

    private static List<string> _readStringTable(BinaryReader reader, uint start, uint size)
    {
        reader.BaseStream.Position = start;
        var end = (long)start + size;
        if (end > reader.BaseStream.Length)
            throw new GameDataException("ARZ string table is outside the archive.");
        var result = new List<string>();
        while (reader.BaseStream.Position < end)
        {
            var count = reader.ReadUInt32();
            for (var index = 0; index < count; index++)
            {
                var length = reader.ReadInt32();
                if (length < 0 || reader.BaseStream.Position + length > end)
                    throw new GameDataException("ARZ string length is invalid.");
                var data = new byte[length];
                reader.BaseStream.ReadExactly(data);
                result.Add(Encoding.UTF8.GetString(data));
            }
        }
        if (reader.BaseStream.Position != end)
            throw new GameDataException("ARZ string table length does not match its header.");
        return result;
    }

    private static List<ArzRecordHeader> _readRecordHeaders(BinaryReader reader, uint start, uint count)
    {
        if (count > int.MaxValue || start > reader.BaseStream.Length)
            throw new GameDataException("ARZ record table header is invalid.");
        reader.BaseStream.Position = start;
        var result = new List<ArzRecordHeader>((int)count);
        for (var index = 0; index < count; index++)
        {
            var recordNameIndex = reader.ReadUInt32();
            var typeLength = reader.ReadInt32();
            if (typeLength < 0)
                throw new GameDataException("ARZ record type length is negative.");
            reader.BaseStream.Position += typeLength;
            var offset = reader.ReadUInt32();
            var compressedSize = reader.ReadInt32();
            var uncompressedSize = reader.ReadInt32();
            reader.BaseStream.Position += 8;
            result.Add(new ArzRecordHeader
            {
                RecordNameIndex = recordNameIndex,
                Offset = offset,
                CompressedSize = compressedSize,
                UncompressedSize = uncompressedSize
            });
        }
        return result;
    }
}
