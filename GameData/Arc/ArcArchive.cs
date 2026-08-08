using System.Text;

namespace GdCli.GameData.Arc;

internal sealed class ArcArchive : IDisposable
{
    private readonly FileStream _stream;
    private readonly List<ArcPart> _parts;
    private readonly IReadOnlyList<ArcEntry> _entries;

    public ArcArchive(string path)
    {
        Path = path;
        _stream = GameFile.OpenRead(path);
        try
        {
            using var reader = new BinaryReader(_stream, Encoding.UTF8, true);
            _ = reader.ReadInt32();
            var version = reader.ReadInt32();
            if (version != 3)
                throw new GameDataException($"Unsupported ARC version in {path}: {version}");
            var fileCount = reader.ReadInt32();
            var partCount = reader.ReadInt32();
            var partTableSize = reader.ReadInt32();
            var stringTableSize = reader.ReadInt32();
            var partTableOffset = reader.ReadInt32();
            if (fileCount < 0 || partCount < 0 || partTableSize < 0 || stringTableSize < 0 || partTableOffset < 0)
                throw new GameDataException($"Invalid ARC header in {path}");

            _parts = _readParts(reader, partTableOffset, partCount);
            var strings = _readStrings(reader, partTableOffset + partTableSize, stringTableSize, fileCount);
            _entries = _readEntries(reader, partTableOffset + partTableSize + stringTableSize, strings);
        }
        catch
        {
            _stream.Dispose();
            throw;
        }
    }

    public string Path { get; }

    public IReadOnlyList<ArcEntry> Entries => _entries;

    public byte[] ReadEntry(string path)
    {
        var entry = _findEntry(path);
        if (entry.DecompressedLength > int.MaxValue)
            throw new GameDataException($"ARC entry is too large to read into memory: {entry.Path}");
        using var output = new MemoryStream((int)entry.DecompressedLength);
        _extract(entry, output);
        return output.ToArray();
    }

    public Stream OpenEntry(string path)
    {
        var entry = _findEntry(path);
        var parts = _parts.Skip(entry.PartIndex).Take(entry.PartCount).ToList();
        if (parts.Count != entry.PartCount)
            throw new GameDataException($"ARC entry has invalid parts: {entry.Path}");
        return new ArcEntryStream(Path, parts, entry.DecompressedLength);
    }

    public void Dispose()
    {
        _stream.Dispose();
    }

    private void _extract(ArcEntry entry, Stream output)
    {
        for (var index = 0; index < entry.PartCount; index++)
        {
            var partIndex = checked(entry.PartIndex + index);
            if (partIndex < 0 || partIndex >= _parts.Count)
                throw new GameDataException($"ARC entry has an invalid part index: {entry.Path}");
            var part = _parts[partIndex];
            _stream.Position = part.Offset;
            var compressed = new byte[part.CompressedLength];
            _stream.ReadExactly(compressed);
            if (part.CompressedLength == part.DecompressedLength)
            {
                output.Write(compressed);
            }
            else
            {
                var decompressed = LZ4.LZ4Codec.Decode(
                    compressed,
                    0,
                    compressed.Length,
                    part.DecompressedLength);
                output.Write(decompressed);
            }
        }

        if (output.Length != entry.DecompressedLength)
            throw new GameDataException(
                $"ARC entry length mismatch in {Path}: {entry.Path}, expected {entry.DecompressedLength}, actual {output.Length}");
    }

    private ArcEntry _findEntry(string path)
    {
        var normalized = path.Replace('\\', '/');
        return _entries.FirstOrDefault(entry => entry.Path.Equals(normalized, StringComparison.OrdinalIgnoreCase))
            ?? throw new GameDataException($"ARC entry was not found: {path} in {Path}");
    }

    private static List<ArcPart> _readParts(BinaryReader reader, int offset, int count)
    {
        reader.BaseStream.Position = offset;
        var result = new List<ArcPart>(count);
        for (var index = 0; index < count; index++)
        {
            var part = new ArcPart
            {
                Offset = reader.ReadInt32(),
                CompressedLength = reader.ReadInt32(),
                DecompressedLength = reader.ReadInt32()
            };
            if (part.Offset < 0 ||
                part.CompressedLength <= 0 ||
                part.DecompressedLength <= 0 ||
                part.Offset + (long)part.CompressedLength > reader.BaseStream.Length)
                throw new GameDataException("ARC part table contains an invalid entry.");
            result.Add(part);
        }
        return result;
    }

    private static List<string> _readStrings(BinaryReader reader, int offset, int size, int count)
    {
        reader.BaseStream.Position = offset;
        var data = reader.ReadBytes(size);
        var result = new List<string>(count);
        var start = 0;
        while (start < data.Length && result.Count < count)
        {
            var end = Array.IndexOf(data, (byte)0, start);
            if (end < 0)
                throw new GameDataException("ARC string table is truncated.");
            result.Add(Encoding.UTF8.GetString(data, start, end - start).Replace('\\', '/'));
            start = end + 1;
        }
        if (result.Count != count)
            throw new GameDataException("ARC string count does not match the header.");
        return result;
    }

    private static List<ArcEntry> _readEntries(BinaryReader reader, int offset, List<string> paths)
    {
        reader.BaseStream.Position = offset;
        var result = new List<ArcEntry>(paths.Count);
        for (var index = 0; index < paths.Count; index++)
        {
            _ = reader.ReadInt32();
            _ = reader.ReadInt32();
            _ = reader.ReadInt32();
            var decompressedLength = reader.ReadUInt32();
            _ = reader.ReadInt32();
            _ = reader.ReadInt64();
            var partCount = reader.ReadInt32();
            var partIndex = reader.ReadInt32();
            _ = reader.ReadInt32();
            _ = reader.ReadInt32();
            result.Add(new ArcEntry
            {
                Path = paths[index],
                DecompressedLength = decompressedLength,
                PartIndex = partIndex,
                PartCount = partCount
            });
        }
        return result;
    }
}
