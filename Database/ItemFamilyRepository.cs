using System.Globalization;
using GdCli.Contracts;
using Microsoft.Data.Sqlite;

namespace GdCli.Database;

internal sealed class ItemFamilyRepository
{
    private const string _familyRowsSql = """
        SELECT
            CASE
                WHEN R.name_tag IS NULL OR R.name_tag = '' THEN 'record:' || R.record_id
                ELSE 'tag:' || R.name_tag
            END AS family_key,
            NULLIF(R.name_tag, '') AS name_tag,
            COALESCE(R.display_name, R.record_id) AS name,
            I.is_mi,
            R.record_id,
            I.rarity,
            I.availability
        FROM items I
        JOIN records R ON R.id = I.record_pk
        WHERE ((@availability IS NOT NULL AND I.availability = @availability COLLATE NOCASE)
               OR (@availability IS NULL AND (@includeUnavailable = 1 OR I.availability <> 'unavailable')))
        """;

    private readonly SqliteConnection _connection;

    public ItemFamilyRepository(SqliteConnection connection)
    {
        _connection = connection;
    }

    public int Count(ItemFamilyFilter filter)
    {
        using var command = _connection.CreateCommand();
        command.CommandText = $"""
            WITH family_rows AS ({_familyRowsSql}),
            families AS (
                SELECT family_key, MAX(is_mi) AS has_mi
                FROM family_rows
                GROUP BY family_key
            )
            SELECT COUNT(*)
            FROM families
            WHERE (@mi IS NULL OR has_mi = @mi)
            """;
        command.Parameters.AddWithValue("@mi", SqliteQuery.Value(filter.HasMiRecord));
        command.Parameters.AddWithValue("@availability", SqliteQuery.Value(filter.Availability));
        command.Parameters.AddWithValue("@includeUnavailable", filter.IncludeUnavailable);
        return Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture);
    }

    public List<ItemFamily> Load(ItemFamilyFilter filter, int offset, int? limit)
    {
        using var command = _connection.CreateCommand();
        command.CommandText = $"""
            WITH family_rows AS ({_familyRowsSql}),
            families AS (
                SELECT
                    family_key,
                    name_tag,
                    MIN(name) AS name,
                    MAX(is_mi) AS has_mi,
                    MIN(is_mi) = 0 AS has_non_mi
                FROM family_rows
                GROUP BY family_key, name_tag
            ),
            family_page AS (
                SELECT *
                FROM families
                WHERE (@mi IS NULL OR has_mi = @mi)
                ORDER BY family_key COLLATE NOCASE
                LIMIT @limit OFFSET @offset
            )
            SELECT
                P.family_key,
                P.name_tag,
                P.name,
                P.has_mi,
                P.has_non_mi,
                F.record_id,
                F.rarity,
                F.availability
            FROM family_page P
            JOIN family_rows F ON F.family_key = P.family_key
            ORDER BY P.family_key COLLATE NOCASE, F.record_id COLLATE NOCASE
            """;
        command.Parameters.AddWithValue("@mi", SqliteQuery.Value(filter.HasMiRecord));
        command.Parameters.AddWithValue("@availability", SqliteQuery.Value(filter.Availability));
        command.Parameters.AddWithValue("@includeUnavailable", filter.IncludeUnavailable);
        SqliteQuery.AddPaging(command, offset, limit);
        return _read(command);
    }

    private static List<ItemFamily> _read(SqliteCommand command)
    {
        using var reader = command.ExecuteReader();
        var result = new List<ItemFamily>();
        string? currentKey = null;
        string? nameTag = null;
        string? name = null;
        var hasMiRecord = false;
        var hasNonMiRecord = false;
        var recordIds = new List<string>();
        var rarities = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var availabilities = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        while (reader.Read())
        {
            var key = reader.GetString(0);
            if (currentKey != null && !string.Equals(currentKey, key, StringComparison.Ordinal))
            {
                result.Add(_create(
                    nameTag,
                    name,
                    hasMiRecord,
                    hasNonMiRecord,
                    recordIds,
                    rarities,
                    availabilities));
                recordIds = [];
                rarities = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                availabilities = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            }

            currentKey = key;
            nameTag = reader.IsDBNull(1) ? null : reader.GetString(1);
            name = reader.GetString(2);
            hasMiRecord = reader.GetBoolean(3);
            hasNonMiRecord = reader.GetBoolean(4);
            recordIds.Add(reader.GetString(5));
            rarities.Add(reader.GetString(6));
            availabilities.Add(reader.GetString(7));
        }

        if (currentKey != null)
        {
            result.Add(_create(
                nameTag,
                name,
                hasMiRecord,
                hasNonMiRecord,
                recordIds,
                rarities,
                availabilities));
        }
        return result;
    }

    private static ItemFamily _create(
        string? nameTag,
        string? name,
        bool hasMiRecord,
        bool hasNonMiRecord,
        IReadOnlyList<string> recordIds,
        IEnumerable<string> rarities,
        IEnumerable<string> availabilities)
    {
        return new ItemFamily
        {
            NameTag = nameTag,
            Name = name ?? string.Empty,
            HasMiRecord = hasMiRecord,
            HasNonMiRecord = hasNonMiRecord,
            RecordIds = recordIds,
            Rarities = rarities.Order(StringComparer.OrdinalIgnoreCase).ToList(),
            Availabilities = availabilities.Order(StringComparer.OrdinalIgnoreCase).ToList()
        };
    }
}
