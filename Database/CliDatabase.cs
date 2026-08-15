using System.Globalization;
using GdCli.Contracts;
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
            ItemSets = new ItemSetRepository(_connection);
            ItemVariants = new ItemVariantRepository(_connection);
            Affixes = new AffixRepository(_connection);
            AffixSkillModifiers = new AffixSkillModifierRepository(_connection);
            Acquisitions = new AcquisitionRepository(_connection);
            Quests = new QuestRepository(_connection);
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

    public ItemSetRepository ItemSets { get; }

    public ItemVariantRepository ItemVariants { get; }

    public AffixRepository Affixes { get; }

    public AffixSkillModifierRepository AffixSkillModifiers { get; }

    public AcquisitionRepository Acquisitions { get; }

    public QuestRepository Quests { get; }

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
            ItemSetCount = _scalar<long>("SELECT COUNT(*) FROM item_sets"),
            AffixCount = _scalar<long>("SELECT COUNT(*) FROM affixes WHERE family = 'standard'"),
            AscendedAffixCount = _scalar<long>("SELECT COUNT(*) FROM affixes WHERE family = 'ascended'"),
            AscendedSkillModifierCount = _countSkillModifiers("ascended"),
            VariantCount = _scalar<long>("SELECT COUNT(*) FROM affixes WHERE family = 'variant'"),
            VariantSkillModifierCount = _countSkillModifiers("variant"),
            LevelCount = _scalar<long>("SELECT COUNT(*) FROM levels"),
            PlacementCount = _scalar<long>("SELECT COUNT(*) FROM placements"),
            QuestCount = _scalar<long>("SELECT COUNT(*) FROM quests"),
            QuestNodeCount = _scalar<long>("SELECT COUNT(*) FROM quest_nodes"),
            QuestEntityCount = _scalar<long>("SELECT COUNT(*) FROM quest_entities"),
            MiRecordCount = miRecordCount,
            MiNameTagCount = _scalar<long>("""
                SELECT COUNT(DISTINCT R.name_tag)
                FROM items I
                JOIN records R ON R.id = I.record_pk
                WHERE I.is_mi = 1 AND R.name_tag IS NOT NULL AND R.name_tag <> ''
                """),
            AcquisitionSourceCount = _scalar<long>("SELECT COUNT(*) FROM acquisition_sources"),
            RecipeCount = _scalar<long>("SELECT COUNT(*) FROM recipes"),
            GameLanguage = _metadata("gameLanguage"),
            GameDirectory = _metadata("gameDirectory"),
            Rarities = GetRarities(),
            ItemClasses = GetItemClasses(),
            AffixFamilies = GetAffixFamilies(),
            AffixKinds = GetAffixKinds(),
            AscendedCategories = GetAscendedCategories(),
            Availabilities = GetAvailabilities()
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
        return _distinct("SELECT kind AS value FROM affixes WHERE family = 'standard'");
    }

    public IReadOnlyList<string> GetAffixFamilies()
    {
        return _distinct(
            "SELECT family AS value FROM affixes WHERE family IN ('standard', 'ascended')");
    }

    public IReadOnlyList<string> GetAscendedCategories()
    {
        return _distinct("SELECT category AS value FROM ascended_affix_categories");
    }

    public IReadOnlyList<string> GetAvailabilities()
    {
        return _distinct("SELECT availability AS value FROM items UNION SELECT availability AS value FROM item_sets");
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
                FROM record_fields F
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
                SELECT record_id, COALESCE(display_name, record_id)
                FROM records
                WHERE record_id IN ({parameters})
                """;
            using var reader = command.ExecuteReader();
            while (reader.Read())
                result[reader.GetString(0)] = reader.GetString(1);
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
        command.CommandText = $"""
            SELECT DISTINCT value
            FROM ({sql}) AS valueset
            WHERE value <> ''
            ORDER BY value COLLATE NOCASE
            """;
        using var reader = command.ExecuteReader();
        var result = new List<string>();
        while (reader.Read())
            result.Add(reader.GetString(0));
        return result;
    }

    private long _countSkillModifiers(string family)
    {
        using var command = _connection.CreateCommand();
        command.CommandText = """
            SELECT COUNT(DISTINCT ASM.modifier_pk)
            FROM affix_skill_modifiers ASM
            JOIN affixes A ON A.record_pk = ASM.affix_pk
            WHERE A.family = @family
            """;
        command.Parameters.AddWithValue("@family", family);
        return Convert.ToInt64(command.ExecuteScalar(), CultureInfo.InvariantCulture);
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
