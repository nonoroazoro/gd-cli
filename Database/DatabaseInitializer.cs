using System.Globalization;
using GdCli.GameData;
using GdCli.GameData.Arc;
using GdCli.GameData.Arz;
using GdCli.GameData.Map;
using GdCli.GameData.Tags;
using Microsoft.Data.Sqlite;

namespace GdCli.Database;

internal static class DatabaseInitializer
{
    public static InitializationResult Initialize(string gameDirectory, string gameLanguage)
    {
        var install = GameInstall.Open(gameDirectory, gameLanguage);
        var targetPath = DatabasePaths.EnsureDirectory();
        using var temporaryDatabase = TemporaryDatabaseFile.Create(targetPath);
        _build(temporaryDatabase.Path, install);
        _replaceDatabase(temporaryDatabase.Path, targetPath);
        return _readResult(targetPath, install);
    }

    private static void _build(string path, GameInstall install)
    {
        var connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = path,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Pooling = false
        }.ToString();
        using var connection = new SqliteConnection(connectionString);
        connection.Open();
        _execute(connection, "PRAGMA journal_mode = OFF; PRAGMA synchronous = OFF; PRAGMA temp_store = MEMORY;");
        _execute(connection, DatabaseSchema.CreateSql);
        using (var transaction = connection.BeginTransaction())
        {
            _saveMetadata(connection, transaction, install);
            _saveSources(connection, transaction, install);
            _importTags(connection, transaction, install);
            _importRecords(connection, transaction, install);
            GameRecordCatalogBuilder.Build(connection, transaction);
            AffixCompatibilityBuilder.Build(connection, transaction);
            AscendedAffixBuilder.Build(connection, transaction);
            _execute(connection, transaction, DatabaseSchema.CreateBuildIndexesSql);
            AcquisitionCatalogBuilder.Build(connection, transaction);
            QuestCatalogBuilder.Build(connection, transaction, install);
            _importMaps(connection, transaction, install);
            transaction.Commit();
        }
        _execute(connection, DatabaseSchema.CreateIndexesSql);
        _execute(connection, "ANALYZE; PRAGMA optimize;");
        using var check = connection.CreateCommand();
        check.CommandText = "PRAGMA integrity_check";
        var result = Convert.ToString(check.ExecuteScalar(), CultureInfo.InvariantCulture);
        if (!string.Equals(result, "ok", StringComparison.OrdinalIgnoreCase))
            throw new GameDataException($"Generated database failed integrity_check: {result}");
    }

    private static void _importTags(SqliteConnection connection, SqliteTransaction transaction, GameInstall install)
    {
        var tags = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var source in install.Sources)
        {
            if (source.EnglishTagsPath != null)
            {
                foreach (var entry in TagArchiveReader.Read(source.EnglishTagsPath))
                    tags[entry.Key] = entry.Value;
            }
        }
        if (install.GameLanguage != "EN")
        {
            foreach (var source in install.Sources)
            {
                if (source.LocalizedTagsPath == null)
                    continue;
                foreach (var entry in TagArchiveReader.Read(source.LocalizedTagsPath))
                    tags[entry.Key] = entry.Value;
            }
        }

        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "INSERT INTO tags(tag, text) VALUES (@tag, @text)";
        var tagParameter = command.Parameters.Add("@tag", SqliteType.Text);
        var textParameter = command.Parameters.Add("@text", SqliteType.Text);
        foreach (var entry in tags)
        {
            tagParameter.Value = entry.Key;
            textParameter.Value = entry.Value;
            command.ExecuteNonQuery();
        }
    }

    private static void _importRecords(SqliteConnection connection, SqliteTransaction transaction, GameInstall install)
    {
        _execute(connection, transaction, """
            CREATE TEMP TABLE raw_references (
                source_pk INTEGER NOT NULL,
                field_pk INTEGER NOT NULL,
                ordinal INTEGER NOT NULL,
                target_record_id TEXT NOT NULL COLLATE NOCASE,
                PRIMARY KEY (source_pk, field_pk, ordinal)
            ) WITHOUT ROWID;
            """);
        using var questResourceIndex = new QuestResourceIndexWriter(connection, transaction);
        var records = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
        var fieldNames = new Dictionary<string, long>(StringComparer.Ordinal);
        using var insertRecord = _command(connection, transaction, """
            INSERT INTO records(record_id, source_name, class, template, name_tag)
            VALUES (@record, @source, @class, @template, @nameTag)
            RETURNING id
            """);
        using var updateRecord = _command(connection, transaction, """
            UPDATE records
            SET source_name = @source, class = @class, template = @template,
                name_tag = @nameTag, display_name = ''
            WHERE id = @id
            """);
        using var insertFieldName = _command(connection, transaction,
            "INSERT INTO field_names(name) VALUES (@name) RETURNING id");
        using var deleteItemFields = _command(connection, transaction, "DELETE FROM item_fields WHERE record_pk = @record");
        using var deleteConditions = _command(connection, transaction, "DELETE FROM loot_conditions WHERE record_pk = @record");
        using var deleteReferences = _command(connection, transaction, "DELETE FROM raw_references WHERE source_pk = @record");
        _ = deleteItemFields.Parameters.Add("@record", SqliteType.Integer);
        _ = deleteConditions.Parameters.Add("@record", SqliteType.Integer);
        _ = deleteReferences.Parameters.Add("@record", SqliteType.Integer);
        using var insertField = _command(connection, transaction, """
            INSERT INTO item_fields(record_pk, field_pk, ordinal, numeric_value, text_value)
            VALUES (@record, @field, @ordinal, @numeric, @text)
            """);
        var fieldRecord = insertField.Parameters.Add("@record", SqliteType.Integer);
        var fieldName = insertField.Parameters.Add("@field", SqliteType.Integer);
        var fieldOrdinal = insertField.Parameters.Add("@ordinal", SqliteType.Integer);
        var fieldNumeric = insertField.Parameters.Add("@numeric", SqliteType.Real);
        var fieldText = insertField.Parameters.Add("@text", SqliteType.Text);
        using var insertCondition = _command(connection, transaction, """
            INSERT INTO loot_conditions(record_pk, field_pk, ordinal, numeric_value, text_value)
            VALUES (@record, @field, @ordinal, @numeric, @text)
            """);
        var conditionRecord = insertCondition.Parameters.Add("@record", SqliteType.Integer);
        var conditionField = insertCondition.Parameters.Add("@field", SqliteType.Integer);
        var conditionOrdinal = insertCondition.Parameters.Add("@ordinal", SqliteType.Integer);
        var conditionNumeric = insertCondition.Parameters.Add("@numeric", SqliteType.Real);
        var conditionText = insertCondition.Parameters.Add("@text", SqliteType.Text);
        using var insertReference = _command(connection, transaction, """
            INSERT INTO raw_references(source_pk, field_pk, ordinal, target_record_id)
            VALUES (@source, @field, @ordinal, @target)
            """);
        var referenceSource = insertReference.Parameters.Add("@source", SqliteType.Integer);
        var referenceField = insertReference.Parameters.Add("@field", SqliteType.Integer);
        var referenceOrdinal = insertReference.Parameters.Add("@ordinal", SqliteType.Integer);
        var referenceTarget = insertReference.Parameters.Add("@target", SqliteType.Text);

        foreach (var source in install.Sources)
        {
            using var archive = new ArzArchiveReader(source.ArzPath);
            foreach (var record in archive.ReadRecords())
            {
                var normalizedRecord = _normalizeRecord(record.RecordId);
                var className = _firstText(record, "Class");
                var templateName = _firstText(record, "templateName");
                var recordNameTag = _firstText(record, "itemNameTag")
                    ?? _firstText(record, "lootRandomizerName")
                    ?? _firstText(record, "description")
                    ?? _firstText(record, "skillDisplayName")
                    ?? _firstText(record, "artifactName")
                    ?? _firstText(record, "monsterName");
                long recordPk;
                if (records.TryGetValue(normalizedRecord, out var existingPk))
                {
                    recordPk = existingPk;
                    updateRecord.Parameters.Clear();
                    updateRecord.Parameters.AddWithValue("@source", source.Name);
                    updateRecord.Parameters.AddWithValue("@class", _dbValue(className));
                    updateRecord.Parameters.AddWithValue("@template", _dbValue(templateName));
                    updateRecord.Parameters.AddWithValue("@nameTag", _dbValue(recordNameTag));
                    updateRecord.Parameters.AddWithValue("@id", recordPk);
                    updateRecord.ExecuteNonQuery();
                }
                else
                {
                    insertRecord.Parameters.Clear();
                    insertRecord.Parameters.AddWithValue("@record", normalizedRecord);
                    insertRecord.Parameters.AddWithValue("@source", source.Name);
                    insertRecord.Parameters.AddWithValue("@class", _dbValue(className));
                    insertRecord.Parameters.AddWithValue("@template", _dbValue(templateName));
                    insertRecord.Parameters.AddWithValue("@nameTag", _dbValue(recordNameTag));
                    recordPk = Convert.ToInt64(insertRecord.ExecuteScalar(), CultureInfo.InvariantCulture);
                    records[normalizedRecord] = recordPk;
                }

                deleteItemFields.Parameters["@record"].Value = recordPk;
                deleteItemFields.ExecuteNonQuery();
                deleteConditions.Parameters["@record"].Value = recordPk;
                deleteConditions.ExecuteNonQuery();
                deleteReferences.Parameters["@record"].Value = recordPk;
                deleteReferences.ExecuteNonQuery();
                questResourceIndex.Reset(recordPk);

                var storeFields = _shouldStoreFields(normalizedRecord);
                foreach (var field in record.Fields)
                {
                    var fieldPk = _getFieldPk(field.Name, fieldNames, insertFieldName);
                    if (storeFields)
                    {
                        fieldRecord.Value = recordPk;
                        fieldName.Value = fieldPk;
                        fieldOrdinal.Value = field.Ordinal;
                        fieldNumeric.Value = field.NumericValue;
                        fieldText.Value = _dbValue(field.TextValue);
                        insertField.ExecuteNonQuery();
                    }
                    if (_isLootCondition(normalizedRecord, field.Name, field.TextValue))
                    {
                        conditionRecord.Value = recordPk;
                        conditionField.Value = fieldPk;
                        conditionOrdinal.Value = field.Ordinal;
                        conditionNumeric.Value = field.NumericValue;
                        conditionText.Value = _dbValue(field.TextValue);
                        insertCondition.ExecuteNonQuery();
                    }
                    if (field.TextValue != null && field.TextValue.EndsWith(".dbr", StringComparison.OrdinalIgnoreCase))
                    {
                        referenceSource.Value = recordPk;
                        referenceField.Value = fieldPk;
                        referenceOrdinal.Value = field.Ordinal;
                        referenceTarget.Value = _normalizeRecord(field.TextValue);
                        insertReference.ExecuteNonQuery();
                    }
                    questResourceIndex.Capture(recordPk, field.TextValue);
                }
            }
        }
        _execute(connection, transaction, """
            INSERT INTO record_references(source_pk, field_pk, ordinal, target_pk)
            SELECT RR.source_pk, RR.field_pk, RR.ordinal, R.id
            FROM raw_references RR
            JOIN records R ON R.record_id = RR.target_record_id;
            DROP TABLE raw_references;
            """);
    }

    private static void _importMaps(SqliteConnection connection, SqliteTransaction transaction, GameInstall install)
    {
        var relevantRecords = _loadRelevantPlacementRecords(connection, transaction);
        using var insertLevel = _command(connection, transaction, """
            INSERT INTO levels(source_name, level_path, rift_gate_record_id, offset_x, offset_y, offset_z)
            VALUES (@source, @path, @rift, @x, @y, @z)
            RETURNING id
            """);
        var levelSource = insertLevel.Parameters.Add("@source", SqliteType.Text);
        var levelPath = insertLevel.Parameters.Add("@path", SqliteType.Text);
        var levelRift = insertLevel.Parameters.Add("@rift", SqliteType.Text);
        var levelX = insertLevel.Parameters.Add("@x", SqliteType.Integer);
        var levelY = insertLevel.Parameters.Add("@y", SqliteType.Integer);
        var levelZ = insertLevel.Parameters.Add("@z", SqliteType.Integer);
        using var insertPlacement = _command(connection, transaction, """
            INSERT INTO placements(level_pk, entity_ordinal, record_pk, world_x, world_y, world_z)
            VALUES (@level, @ordinal, @record, @wx, @wy, @wz)
            """);
        var placementLevel = insertPlacement.Parameters.Add("@level", SqliteType.Integer);
        var placementOrdinal = insertPlacement.Parameters.Add("@ordinal", SqliteType.Integer);
        var placementRecord = insertPlacement.Parameters.Add("@record", SqliteType.Integer);
        var worldX = insertPlacement.Parameters.Add("@wx", SqliteType.Real);
        var worldY = insertPlacement.Parameters.Add("@wy", SqliteType.Real);
        var worldZ = insertPlacement.Parameters.Add("@wz", SqliteType.Real);

        var source = install.Sources
            .Where(candidate => candidate.LevelsPath != null)
            .OrderByDescending(candidate => candidate.Priority)
            .FirstOrDefault()
            ?? throw new GameDataException("No Levels.arc was found in the game data sources.");
        var levelsPath = source.LevelsPath
            ?? throw new GameDataException("The selected game data source has no Levels.arc path.");
        using (var archive = new ArcArchive(levelsPath))
        {
            var mapEntry = archive.Entries.FirstOrDefault(entry => entry.Path.EndsWith("world001.map", StringComparison.OrdinalIgnoreCase))
                ?? throw new GameDataException($"world001.map was not found in {levelsPath}");
            using var mapStream = archive.OpenEntry(mapEntry.Path);
            using var map = new WorldMapReader(mapStream, $"{levelsPath}::{mapEntry.Path}", true);
            var levelIds = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
            foreach (var level in map.Levels)
            {
                levelSource.Value = source.Name;
                levelPath.Value = level.Path;
                levelRift.Value = level.RiftGateRecordId;
                levelX.Value = level.OffsetX;
                levelY.Value = level.OffsetY;
                levelZ.Value = level.OffsetZ;
                levelIds[level.Path] = Convert.ToInt64(insertLevel.ExecuteScalar(), CultureInfo.InvariantCulture);
            }

            foreach (var placement in map.ReadPlacements(relevantRecords.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase)))
            {
                if (!levelIds.TryGetValue(placement.LevelPath, out var levelPk) ||
                    !relevantRecords.TryGetValue(_normalizeRecord(placement.RecordId), out var recordPk))
                    continue;
                placementLevel.Value = levelPk;
                placementOrdinal.Value = placement.EntityOrdinal;
                placementRecord.Value = recordPk;
                worldX.Value = placement.WorldX;
                worldY.Value = placement.WorldY;
                worldZ.Value = placement.WorldZ;
                insertPlacement.ExecuteNonQuery();
            }
        }
    }

    private static Dictionary<string, long> _loadRelevantPlacementRecords(SqliteConnection connection, SqliteTransaction transaction)
    {
        using var command = _command(connection, transaction, """
            SELECT R.id, R.record_id
            FROM records R
            WHERE R.id IN (
                    SELECT source_pk
                    FROM acquisition_sources
                    WHERE source_pk IS NOT NULL
                )
               OR R.id IN (SELECT record_pk FROM quest_entities)
               OR R.id IN (SELECT placed_pk FROM entity_aliases)
               OR ((R.record_id LIKE 'records/proxies/%' OR R.record_id LIKE 'records/creatures/%')
                    AND EXISTS (SELECT 1 FROM record_references RR WHERE RR.source_pk = R.id))
            """);
        using var reader = command.ExecuteReader();
        var result = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
        while (reader.Read())
            result[reader.GetString(1)] = reader.GetInt64(0);
        return result;
    }

    private static void _saveMetadata(SqliteConnection connection, SqliteTransaction transaction, GameInstall install)
    {
        using var command = _command(connection, transaction, "INSERT INTO metadata(key, value) VALUES (@key, @value)");
        var key = command.Parameters.Add("@key", SqliteType.Text);
        var value = command.Parameters.Add("@value", SqliteType.Text);
        foreach (var entry in new Dictionary<string, string>
        {
            ["schemaVersion"] = DatabaseSchema.Version.ToString(CultureInfo.InvariantCulture),
            ["createdUtc"] = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture),
            ["gameDirectory"] = install.Root,
            ["gameLanguage"] = install.GameLanguage
        })
        {
            key.Value = entry.Key;
            value.Value = entry.Value;
            command.ExecuteNonQuery();
        }
    }

    private static void _saveSources(SqliteConnection connection, SqliteTransaction transaction, GameInstall install)
    {
        using var command = _command(connection, transaction, """
            INSERT INTO sources(
                name, priority, root_path, arz_path, arz_size, arz_modified_utc,
                levels_path, levels_size, levels_modified_utc)
            VALUES (@name, @priority, @root, @arz, @arzSize, @arzModified, @levels, @levelsSize, @levelsModified)
            """);
        foreach (var source in install.Sources)
        {
            var arz = new FileInfo(source.ArzPath);
            var levels = source.LevelsPath == null ? null : new FileInfo(source.LevelsPath);
            command.Parameters.Clear();
            command.Parameters.AddWithValue("@name", source.Name);
            command.Parameters.AddWithValue("@priority", source.Priority);
            command.Parameters.AddWithValue("@root", source.Root);
            command.Parameters.AddWithValue("@arz", source.ArzPath);
            command.Parameters.AddWithValue("@arzSize", arz.Length);
            command.Parameters.AddWithValue("@arzModified", arz.LastWriteTimeUtc.ToString("O", CultureInfo.InvariantCulture));
            command.Parameters.AddWithValue("@levels", _dbValue(source.LevelsPath));
            command.Parameters.AddWithValue("@levelsSize", levels == null ? DBNull.Value : levels.Length);
            command.Parameters.AddWithValue("@levelsModified", levels == null
                ? DBNull.Value
                : levels.LastWriteTimeUtc.ToString("O", CultureInfo.InvariantCulture));
            command.ExecuteNonQuery();
        }
    }

    private static InitializationResult _readResult(string path, GameInstall install)
    {
        var connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = path,
            Mode = SqliteOpenMode.ReadOnly,
            Pooling = false
        }.ToString();
        using var connection = new SqliteConnection(connectionString);
        connection.Open();
        return new InitializationResult
        {
            Database = path,
            GameDirectory = install.Root,
            GameLanguage = install.GameLanguage,
            Sources = install.Sources.Count,
            Records = _scalar(connection, "SELECT COUNT(*) FROM records"),
            ItemFields = _scalar(connection, "SELECT COUNT(*) FROM item_fields"),
            LootGraphEdges = _scalar(connection, "SELECT COUNT(*) FROM record_references"),
            LootConditions = _scalar(connection, "SELECT COUNT(*) FROM loot_conditions"),
            Items = _scalar(connection, "SELECT COUNT(*) FROM items"),
            Affixes = _scalar(connection, "SELECT COUNT(*) FROM affixes"),
            AscendedAffixes = _scalar(connection, "SELECT COUNT(*) FROM ascended_affixes"),
            AscendedSkillModifiers = _scalar(
                connection,
                "SELECT COUNT(DISTINCT modifier_pk) FROM ascended_skill_modifiers"),
            AffixCompatibilityRelations = _scalar(connection, "SELECT COUNT(*) FROM affix_item_classes"),
            Levels = _scalar(connection, "SELECT COUNT(*) FROM levels"),
            Placements = _scalar(connection, "SELECT COUNT(*) FROM placements"),
            AcquisitionSources = _scalar(connection, "SELECT COUNT(*) FROM acquisition_sources"),
            Recipes = _scalar(connection, "SELECT COUNT(*) FROM recipes"),
            Quests = _scalar(connection, "SELECT COUNT(*) FROM quests"),
            QuestNodes = _scalar(connection, "SELECT COUNT(*) FROM quest_nodes"),
            QuestEntities = _scalar(connection, "SELECT COUNT(*) FROM quest_entities"),
            FileSize = new FileInfo(path).Length
        };
    }

    private static long _scalar(SqliteConnection connection, string sql)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        return Convert.ToInt64(command.ExecuteScalar(), CultureInfo.InvariantCulture);
    }

    private static SqliteCommand _command(SqliteConnection connection, SqliteTransaction transaction, string sql)
    {
        var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        return command;
    }

    private static void _execute(SqliteConnection connection, string sql)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }

    private static void _execute(SqliteConnection connection, SqliteTransaction transaction, string sql)
    {
        using var command = _command(connection, transaction, sql);
        command.ExecuteNonQuery();
    }

    private static string? _firstText(ArzRecord record, string name)
    {
        return record.Fields.FirstOrDefault(field => field.Name == name)?.TextValue;
    }

    private static long _getFieldPk(
        string name,
        Dictionary<string, long> fieldNames,
        SqliteCommand insertFieldName)
    {
        if (fieldNames.TryGetValue(name, out var fieldPk))
            return fieldPk;
        insertFieldName.Parameters.Clear();
        insertFieldName.Parameters.AddWithValue("@name", name);
        fieldPk = Convert.ToInt64(insertFieldName.ExecuteScalar(), CultureInfo.InvariantCulture);
        fieldNames[name] = fieldPk;
        return fieldPk;
    }

    private static bool _shouldStoreFields(string recordId)
    {
        return (recordId.StartsWith("records/items/", StringComparison.OrdinalIgnoreCase) &&
                !recordId.StartsWith("records/items/loottables/", StringComparison.OrdinalIgnoreCase)) ||
               recordId.Contains("/skillmodifiers/ascended/", StringComparison.OrdinalIgnoreCase);
    }

    private static bool _isLootCondition(string recordId, string field, string? textValue)
    {
        if (textValue != null && textValue.EndsWith(".dbr", StringComparison.OrdinalIgnoreCase))
            return false;
        if (recordId.StartsWith("records/items/loottables/", StringComparison.OrdinalIgnoreCase))
        {
            return field.StartsWith("lootWeight", StringComparison.OrdinalIgnoreCase) ||
                   field.StartsWith("lootChance", StringComparison.OrdinalIgnoreCase) ||
                   field.Equals("forceHighestLevel", StringComparison.OrdinalIgnoreCase) ||
                   field.Equals("minItemLevelEquation", StringComparison.OrdinalIgnoreCase) ||
                   field.Equals("maxItemLevelEquation", StringComparison.OrdinalIgnoreCase);
        }
        if (recordId.StartsWith("records/creatures/", StringComparison.OrdinalIgnoreCase))
        {
            return field.StartsWith("chanceToEquip", StringComparison.OrdinalIgnoreCase) ||
                   field.StartsWith("loot", StringComparison.OrdinalIgnoreCase) ||
                   field.Equals("charLevel", StringComparison.OrdinalIgnoreCase);
        }
        if (recordId.StartsWith("records/proxies/", StringComparison.OrdinalIgnoreCase))
        {
            return field.StartsWith("weight", StringComparison.OrdinalIgnoreCase) ||
                   field.StartsWith("levelVarianceEquation", StringComparison.OrdinalIgnoreCase) ||
                   field.Equals("chanceToRun", StringComparison.OrdinalIgnoreCase);
        }
        return false;
    }

    private static string _normalizeRecord(string value)
    {
        return value.Replace('\\', '/');
    }

    private static object _dbValue(object? value)
    {
        return value ?? DBNull.Value;
    }

    private static void _replaceDatabase(string temporaryPath, string targetPath)
    {
        if (File.Exists(targetPath))
            File.Replace(temporaryPath, targetPath, null, true);
        else
            File.Move(temporaryPath, targetPath);
    }
}
