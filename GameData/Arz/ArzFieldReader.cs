using System.Buffers.Binary;

namespace GdCli.GameData.Arz;

internal static class ArzFieldReader
{
    public static IReadOnlyList<ArzField> Read(
        ReadOnlySpan<byte> data,
        IReadOnlyList<string> strings,
        string recordId)
    {
        var fields = new List<ArzField>();
        var fieldOrdinals = new Dictionary<string, int>(StringComparer.Ordinal);
        var offset = 0;
        while (offset < data.Length)
        {
            if (offset + 8 > data.Length)
                throw new GameDataException($"Truncated ARZ field block in {recordId}");

            var type = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(offset, 2));
            var count = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(offset + 2, 2));
            var nameIndex = BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(offset + 4, 4));
            if (nameIndex >= strings.Count)
                throw new GameDataException($"Invalid ARZ field name index: {nameIndex}");
            var name = strings[(int)nameIndex];
            offset += 8;
            for (var index = 0; index < count; index++)
            {
                if (offset + 4 > data.Length)
                    throw new GameDataException($"Truncated ARZ field value in {recordId}");
                var raw = BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(offset, 4));
                offset += 4;
                var ordinal = fieldOrdinals.GetValueOrDefault(name);
                fieldOrdinals[name] = ordinal + 1;

                var value = _decodeValue(type, raw, strings, name);
                if (value == null)
                    continue;
                fields.Add(new ArzField
                {
                    Name = name,
                    Ordinal = ordinal,
                    NumericValue = value.Value.Numeric,
                    TextValue = value.Value.Text
                });
            }
        }
        return fields;
    }

    private static (double Numeric, string? Text)? _decodeValue(
        ushort type,
        uint raw,
        IReadOnlyList<string> strings,
        string fieldName)
    {
        if (type == 1)
        {
            var value = BitConverter.Int32BitsToSingle(unchecked((int)raw));
            return value == 0f ? null : (value, null);
        }
        if (type == 2)
        {
            if (raw >= strings.Count)
                throw new GameDataException($"Invalid ARZ string index: {raw}");
            var value = strings[(int)raw];
            return string.IsNullOrEmpty(value) ? null : (0, value);
        }
        if (type == 0)
        {
            var value = unchecked((int)raw);
            return value == 0 ? null : (value, null);
        }
        if (type == 3)
            return raw == 0 ? null : (raw, null);
        throw new GameDataException($"Unsupported ARZ field type {type}: {fieldName}");
    }
}
