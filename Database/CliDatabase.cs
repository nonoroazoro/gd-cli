using System.Globalization;
using GdCli.Contracts;
using GdCli.Features.Drops;
using Microsoft.Data.Sqlite;

namespace GdCli.Database;

internal sealed class CliDatabase : IDisposable
{
    private readonly SqliteConnection _connection;

    public CliDatabase(string path)
    {
        Path = path;
        var connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = path,
            Mode = SqliteOpenMode.ReadOnly,
            Pooling = false
        }.ToString();
        _connection = new SqliteConnection(connectionString);
        try
        {
            _connection.Open();
            _validateSchema();
            Items = new ItemRepository(_connection);
            ItemFamilies = new ItemFamilyRepository(_connection);
            Affixes = new AffixRepository(_connection);
            AscendedAffixes = new AscendedAffixRepository(_connection);
            Search = new SearchRepository(_connection);
        }
        catch (SqliteException exception)
        {
            _connection.Dispose();
            throw new IncompatibleDatabaseException($"Unable to open the CLI database read-only: {exception.Message}");
        }
        catch
        {
            _connection.Dispose();
            throw;
        }
    }

    public string Path { get; }

    public ItemRepository Items { get; }

    public ItemFamilyRepository ItemFamilies { get; }

    public AffixRepository Affixes { get; }

    public AscendedAffixRepository AscendedAffixes { get; }

    public SearchRepository Search { get; }

    public DatabaseInfo GetInfo()
    {
        var file = new FileInfo(Path);
        var miRecordCount = _scalar<long>("SELECT COUNT(*) FROM items WHERE is_mi = 1");
        return new DatabaseInfo
        {
            Database = Path,
            FileSize = file.Length,
            LastWriteTimeUtc = file.LastWriteTimeUtc,
            SqliteVersion = _scalar<string>("SELECT sqlite_version()"),
            UserVersion = _scalar<long>("PRAGMA user_version"),
            RecordCount = _scalar<long>("SELECT COUNT(*) FROM records"),
            ItemCount = _scalar<long>("SELECT COUNT(*) FROM items"),
            AffixCount = _scalar<long>("SELECT COUNT(*) FROM affixes"),
            AscendedAffixCount = _scalar<long>("SELECT COUNT(*) FROM ascended_affixes"),
            AscendedSkillModifierCount = _scalar<long>(
                "SELECT COUNT(DISTINCT modifier_pk) FROM ascended_skill_modifiers"),
            LevelCount = _scalar<long>("SELECT COUNT(*) FROM levels"),
            PlacementCount = _scalar<long>("SELECT COUNT(*) FROM placements"),
            MiCount = miRecordCount,
            MiRecordCount = miRecordCount,
            MiNameTagCount = _scalar<long>("""
                SELECT COUNT(DISTINCT R.name_tag)
                FROM items I
                JOIN records R ON R.id = I.record_pk
                WHERE I.is_mi = 1 AND R.name_tag IS NOT NULL AND R.name_tag <> ''
                """),
            GameLanguage = _metadata("gameLanguage"),
            GameDirectory = _metadata("gameDirectory"),
            Rarities = GetRarities(),
            ItemClasses = GetItemClasses(),
            AffixKinds = GetAffixKinds(),
            AscendedCategories = GetAscendedCategories()
        };
    }

    public IReadOnlyList<string> GetRarities()
    {
        return _distinct("SELECT rarity AS value FROM items UNION SELECT rarity AS value FROM affixes");
    }

    public IReadOnlyList<string> GetItemClasses()
    {
        return _distinct("SELECT item_class AS value FROM items");
    }

    public IReadOnlyList<string> GetAffixKinds()
    {
        return _distinct("SELECT kind AS value FROM affixes");
    }

    public IReadOnlyList<string> GetAscendedCategories()
    {
        return _distinct("SELECT category AS value FROM ascended_affix_categories");
    }

    public Dictionary<string, List<RawStat>> LoadStats(IEnumerable<string> records)
    {
        var result = new Dictionary<string, List<RawStat>>(StringComparer.OrdinalIgnoreCase);
        foreach (var chunk in records.Distinct(StringComparer.OrdinalIgnoreCase).Chunk(400))
        {
            using var command = _connection.CreateCommand();
            var parameters = SqliteQuery.AddValues(command, "record", chunk);
            command.CommandText = $"""
                SELECT R.record_id, N.name, F.numeric_value, F.text_value
                FROM item_fields F
                JOIN records R ON R.id = F.record_pk
                JOIN field_names N ON N.id = F.field_pk
                WHERE R.record_id IN ({parameters})
                ORDER BY R.record_id, N.name, F.ordinal
                """;
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var record = reader.GetString(0);
                if (!result.TryGetValue(record, out var stats))
                {
                    stats = [];
                    result[record] = stats;
                }
                stats.Add(new RawStat
                {
                    Field = reader.GetString(1),
                    Value = reader.GetDouble(2),
                    TextValue = reader.IsDBNull(3) ? null : reader.GetString(3)
                });
            }
        }
        return result;
    }

    public Dictionary<string, string> LoadTags()
    {
        using var command = _connection.CreateCommand();
        command.CommandText = "SELECT tag, text FROM tags";
        using var reader = command.ExecuteReader();
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        while (reader.Read())
            result[reader.GetString(0)] = reader.GetString(1);
        return result;
    }

    public Dictionary<string, string> LoadRecordNames(IEnumerable<string> records)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var chunk in records.Distinct(StringComparer.OrdinalIgnoreCase).Chunk(400))
        {
            using var command = _connection.CreateCommand();
            var parameters = SqliteQuery.AddValues(command, "record", chunk);
            command.CommandText = $"""
                SELECT record_id, display_name
                FROM records
                WHERE record_id IN ({parameters})
                """;
            using var reader = command.ExecuteReader();
            while (reader.Read())
                result[reader.GetString(0)] = reader.GetString(1);
        }
        return result;
    }

    public Dictionary<string, List<MonsterSource>> LoadMiSources(IEnumerable<string> itemRecords)
    {
        var result = new Dictionary<string, List<MonsterSource>>(StringComparer.OrdinalIgnoreCase);
        foreach (var chunk in itemRecords.Distinct(StringComparer.OrdinalIgnoreCase).Chunk(400))
        {
            using var command = _connection.CreateCommand();
            var parameters = SqliteQuery.AddValues(command, "item", chunk);
            command.CommandText = $"""
                SELECT I.record_id, M.record_id, M.display_name
                FROM monster_drops MD
                JOIN records I ON I.id = MD.item_pk
                JOIN records M ON M.id = MD.monster_pk
                WHERE I.record_id IN ({parameters})
                ORDER BY M.display_name COLLATE NOCASE, M.record_id
                """;
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var item = reader.GetString(0);
                if (!result.TryGetValue(item, out var sources))
                {
                    sources = [];
                    result[item] = sources;
                }
                sources.Add(new MonsterSource
                {
                    RecordId = reader.GetString(1),
                    Name = reader.GetString(2)
                });
            }
        }
        return result;
    }

    public IReadOnlyList<DropReference> LoadReverseDropReferences(string targetRecordId)
    {
        using var command = _connection.CreateCommand();
        command.CommandText = """
            SELECT S.record_id, S.display_name, COALESCE(S.class, ''), N.name, T.record_id
            FROM record_references RR
            JOIN records S ON S.id = RR.source_pk
            JOIN records T ON T.id = RR.target_pk
            JOIN field_names N ON N.id = RR.field_pk
            WHERE T.record_id = @target
              AND (
                  S.record_id LIKE 'records/items/loottables/%'
                  OR S.record_id LIKE 'records/creatures/%'
                  OR S.record_id LIKE 'records/proxies/%'
              )
            ORDER BY S.record_id, N.name, RR.ordinal
            """;
        command.Parameters.AddWithValue("@target", targetRecordId);
        using var reader = command.ExecuteReader();
        var result = new List<DropReference>();
        while (reader.Read())
        {
            result.Add(new DropReference
            {
                SourceRecordId = reader.GetString(0),
                SourceName = reader.GetString(1),
                SourceClass = reader.GetString(2),
                Field = reader.GetString(3),
                TargetRecordId = reader.GetString(4)
            });
        }
        return result;
    }

    public IReadOnlyList<DropCondition> LoadDropConditions(string recordId)
    {
        using var command = _connection.CreateCommand();
        command.CommandText = """
            SELECT N.name, C.numeric_value, C.text_value
            FROM drop_conditions C
            JOIN records R ON R.id = C.record_pk
            JOIN field_names N ON N.id = C.field_pk
            WHERE R.record_id = @record
            ORDER BY N.name, C.ordinal
            """;
        command.Parameters.AddWithValue("@record", recordId);
        using var reader = command.ExecuteReader();
        var result = new List<DropCondition>();
        while (reader.Read())
            result.Add(new DropCondition
            {
                Field = reader.GetString(0),
                Value = reader.GetDouble(1),
                TextValue = reader.IsDBNull(2) ? null : reader.GetString(2)
            });
        return result;
    }

    public IReadOnlyList<DropLocation> LoadLocations(string recordId)
    {
        using var command = _connection.CreateCommand();
        command.CommandText = """
            SELECT L.source_name, L.level_path, L.rift_gate_record_id, P.world_x, P.world_y, P.world_z
            FROM placements P
            JOIN levels L ON L.id = P.level_pk
            JOIN records R ON R.id = P.record_pk
            WHERE R.record_id = @record
            ORDER BY L.source_name, L.level_path, P.entity_ordinal
            """;
        command.Parameters.AddWithValue("@record", recordId);
        using var reader = command.ExecuteReader();
        var result = new List<DropLocation>();
        while (reader.Read())
        {
            result.Add(new DropLocation
            {
                Source = reader.GetString(0),
                Level = reader.GetString(1),
                RiftGateRecordId = reader.GetString(2),
                X = reader.GetDouble(3),
                Y = reader.GetDouble(4),
                Z = reader.GetDouble(5)
            });
        }
        return result;
    }

    public void Dispose()
    {
        _connection.Dispose();
    }

    private void _validateSchema()
    {
        var version = _scalar<long>("PRAGMA user_version");
        if (version != DatabaseSchema.Version)
            throw new IncompatibleDatabaseException($"CLI database schema {version} is incompatible with required schema {DatabaseSchema.Version}. Run init again.");
        foreach (var table in DatabaseSchema.RequiredTables)
        {
            using var command = _connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = @name";
            command.Parameters.AddWithValue("@name", table);
            if (Convert.ToInt64(command.ExecuteScalar(), CultureInfo.InvariantCulture) != 1)
                throw new IncompatibleDatabaseException($"Required table is missing: {table}");
        }
    }

    private string _metadata(string key)
    {
        using var command = _connection.CreateCommand();
        command.CommandText = "SELECT value FROM metadata WHERE key = @key";
        command.Parameters.AddWithValue("@key", key);
        return Convert.ToString(command.ExecuteScalar(), CultureInfo.InvariantCulture) ?? string.Empty;
    }

    private List<string> _distinct(string sql)
    {
        using var command = _connection.CreateCommand();
        command.CommandText = $"SELECT DISTINCT value FROM ({sql}) AS valueset WHERE value <> '' ORDER BY value COLLATE NOCASE";
        using var reader = command.ExecuteReader();
        var result = new List<string>();
        while (reader.Read())
            result.Add(reader.GetString(0));
        return result;
    }

    private T _scalar<T>(string sql)
    {
        using var command = _connection.CreateCommand();
        command.CommandText = sql;
        var value = command.ExecuteScalar();
        if (value == null || value == DBNull.Value)
            throw new IncompatibleDatabaseException($"The database returned no value for: {sql}");
        return (T)Convert.ChangeType(value, typeof(T), CultureInfo.InvariantCulture);
    }

}
