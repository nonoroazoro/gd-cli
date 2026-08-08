namespace GdCli.GameData.Arc;

internal sealed class ArcEntryStream : Stream
{
    private readonly FileStream _archive;
    private readonly IReadOnlyList<ArcPart> _parts;
    private readonly long[] _partOffsets;
    private readonly long _length;
    private long _position;
    private int _cachedPartIndex = -1;
    private byte[] _cachedPart = [];

    public ArcEntryStream(string archivePath, IReadOnlyList<ArcPart> parts, long length)
    {
        _archive = GameFile.OpenRead(archivePath);
        try
        {
            _parts = parts;
            _length = length;
            _partOffsets = new long[parts.Count + 1];
            for (var index = 0; index < parts.Count; index++)
                _partOffsets[index + 1] = _partOffsets[index] + parts[index].DecompressedLength;
            if (_partOffsets[^1] != length)
                throw new GameDataException($"ARC part lengths do not match the entry length: {archivePath}");
        }
        catch
        {
            _archive.Dispose();
            throw;
        }
    }

    public override bool CanRead => true;

    public override bool CanSeek => true;

    public override bool CanWrite => false;

    public override long Length => _length;

    public override long Position
    {
        get => _position;
        set => Seek(value, SeekOrigin.Begin);
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        return Read(buffer.AsSpan(offset, count));
    }

    public override int Read(Span<byte> buffer)
    {
        if (_position >= _length || buffer.Length == 0)
            return 0;
        var total = 0;
        while (buffer.Length > 0 && _position < _length)
        {
            var partIndex = _findPart(_position);
            var part = _loadPart(partIndex);
            var offset = checked((int)(_position - _partOffsets[partIndex]));
            var count = Math.Min(part.Length - offset, buffer.Length);
            part.AsSpan(offset, count).CopyTo(buffer);
            buffer = buffer[count..];
            _position += count;
            total += count;
        }
        return total;
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        var position = origin switch
        {
            SeekOrigin.Begin => offset,
            SeekOrigin.Current => _position + offset,
            SeekOrigin.End => _length + offset,
            _ => throw new ArgumentOutOfRangeException(nameof(origin))
        };
        if (position < 0 || position > _length)
            throw new IOException("Attempted to seek outside the ARC entry.");
        _position = position;
        return position;
    }

    public override void Flush()
    {
    }

    public override void SetLength(long value)
    {
        throw new NotSupportedException();
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        throw new NotSupportedException();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _archive.Dispose();
        base.Dispose(disposing);
    }

    private int _findPart(long position)
    {
        var index = Array.BinarySearch(_partOffsets, position);
        if (index >= 0)
            return Math.Min(index, _parts.Count - 1);
        return ~index - 1;
    }

    private byte[] _loadPart(int index)
    {
        if (_cachedPartIndex == index)
            return _cachedPart;
        var part = _parts[index];
        _archive.Position = part.Offset;
        var compressed = new byte[part.CompressedLength];
        _archive.ReadExactly(compressed);
        _cachedPart = part.CompressedLength == part.DecompressedLength
            ? compressed
            : LZ4.LZ4Codec.Decode(compressed, 0, compressed.Length, part.DecompressedLength);
        _cachedPartIndex = index;
        return _cachedPart;
    }
}
