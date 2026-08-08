namespace GdCli.Database;

internal static class DatabaseSchema
{
    public const int Version = 2;

    public static IReadOnlyList<string> RequiredTables { get; } = Array.AsReadOnly<string>(
    [
        "metadata",
        "sources",
        "tags",
        "records",
        "field_names",
        "item_fields",
        "record_references",
        "drop_conditions",
        "items",
        "affixes",
        "affix_item_classes",
        "ascended_affixes",
        "ascended_affix_categories",
        "ascended_skill_modifiers",
        "monster_drops",
        "levels",
        "placements"
    ]);

    public static readonly string CreateSql = $$"""
        PRAGMA page_size = 32768;
        PRAGMA foreign_keys = ON;
        PRAGMA user_version = {{Version}};

        CREATE TABLE metadata (
            key TEXT PRIMARY KEY,
            value TEXT NOT NULL
        ) WITHOUT ROWID;

        CREATE TABLE sources (
            name TEXT PRIMARY KEY,
            priority INTEGER NOT NULL,
            root_path TEXT NOT NULL,
            arz_path TEXT NOT NULL,
            arz_size INTEGER NOT NULL,
            arz_modified_utc TEXT NOT NULL,
            levels_path TEXT,
            levels_size INTEGER,
            levels_modified_utc TEXT
        ) WITHOUT ROWID;

        CREATE TABLE tags (
            tag TEXT PRIMARY KEY,
            text TEXT NOT NULL
        ) WITHOUT ROWID;

        CREATE TABLE records (
            id INTEGER PRIMARY KEY,
            record_id TEXT NOT NULL UNIQUE COLLATE NOCASE,
            source_name TEXT NOT NULL,
            class TEXT,
            template TEXT,
            name_tag TEXT,
            display_name TEXT NOT NULL DEFAULT ''
        );

        CREATE TABLE field_names (
            id INTEGER PRIMARY KEY,
            name TEXT NOT NULL UNIQUE
        );

        CREATE TABLE item_fields (
            record_pk INTEGER NOT NULL,
            field_pk INTEGER NOT NULL,
            ordinal INTEGER NOT NULL,
            numeric_value REAL NOT NULL,
            text_value TEXT,
            PRIMARY KEY (record_pk, field_pk, ordinal),
            FOREIGN KEY (record_pk) REFERENCES records(id) ON DELETE CASCADE,
            FOREIGN KEY (field_pk) REFERENCES field_names(id)
        ) WITHOUT ROWID;

        CREATE TABLE record_references (
            source_pk INTEGER NOT NULL,
            field_pk INTEGER NOT NULL,
            ordinal INTEGER NOT NULL,
            target_pk INTEGER NOT NULL,
            PRIMARY KEY (source_pk, field_pk, ordinal),
            FOREIGN KEY (source_pk) REFERENCES records(id) ON DELETE CASCADE,
            FOREIGN KEY (field_pk) REFERENCES field_names(id),
            FOREIGN KEY (target_pk) REFERENCES records(id)
        ) WITHOUT ROWID;

        CREATE TABLE drop_conditions (
            record_pk INTEGER NOT NULL,
            field_pk INTEGER NOT NULL,
            ordinal INTEGER NOT NULL,
            numeric_value REAL NOT NULL,
            text_value TEXT,
            PRIMARY KEY (record_pk, field_pk, ordinal),
            FOREIGN KEY (record_pk) REFERENCES records(id) ON DELETE CASCADE,
            FOREIGN KEY (field_pk) REFERENCES field_names(id)
        ) WITHOUT ROWID;

        CREATE TABLE items (
            record_pk INTEGER PRIMARY KEY,
            name TEXT NOT NULL,
            rarity TEXT NOT NULL,
            item_class TEXT NOT NULL,
            item_level REAL NOT NULL,
            required_level REAL NOT NULL,
            is_mi INTEGER NOT NULL DEFAULT 0,
            FOREIGN KEY (record_pk) REFERENCES records(id) ON DELETE CASCADE
        );

        CREATE TABLE affixes (
            record_pk INTEGER PRIMARY KEY,
            name TEXT NOT NULL,
            kind TEXT NOT NULL,
            rarity TEXT NOT NULL,
            item_level REAL NOT NULL,
            required_level REAL NOT NULL,
            jitter_percent REAL NOT NULL,
            FOREIGN KEY (record_pk) REFERENCES records(id) ON DELETE CASCADE
        );

        CREATE TABLE affix_item_classes (
            item_class TEXT NOT NULL COLLATE NOCASE,
            affix_pk INTEGER NOT NULL,
            PRIMARY KEY (item_class, affix_pk),
            FOREIGN KEY (affix_pk) REFERENCES affixes(record_pk) ON DELETE CASCADE
        ) WITHOUT ROWID;

        CREATE TABLE ascended_affixes (
            record_pk INTEGER PRIMARY KEY,
            FOREIGN KEY (record_pk) REFERENCES records(id) ON DELETE CASCADE
        );

        CREATE TABLE ascended_affix_categories (
            affix_pk INTEGER NOT NULL,
            category TEXT NOT NULL COLLATE NOCASE,
            group_name TEXT NOT NULL COLLATE NOCASE,
            PRIMARY KEY (affix_pk, category, group_name),
            FOREIGN KEY (affix_pk) REFERENCES ascended_affixes(record_pk) ON DELETE CASCADE
        ) WITHOUT ROWID;

        CREATE TABLE ascended_skill_modifiers (
            affix_pk INTEGER NOT NULL,
            modifier_pk INTEGER NOT NULL,
            PRIMARY KEY (affix_pk, modifier_pk),
            FOREIGN KEY (affix_pk) REFERENCES ascended_affixes(record_pk) ON DELETE CASCADE,
            FOREIGN KEY (modifier_pk) REFERENCES records(id)
        ) WITHOUT ROWID;

        CREATE TABLE monster_drops (
            item_pk INTEGER NOT NULL,
            monster_pk INTEGER NOT NULL,
            PRIMARY KEY (item_pk, monster_pk),
            FOREIGN KEY (item_pk) REFERENCES items(record_pk) ON DELETE CASCADE,
            FOREIGN KEY (monster_pk) REFERENCES records(id)
        ) WITHOUT ROWID;

        CREATE TABLE levels (
            id INTEGER PRIMARY KEY,
            source_name TEXT NOT NULL,
            level_path TEXT NOT NULL COLLATE NOCASE,
            rift_gate_record_id TEXT NOT NULL COLLATE NOCASE,
            offset_x INTEGER NOT NULL,
            offset_y INTEGER NOT NULL,
            offset_z INTEGER NOT NULL,
            UNIQUE (source_name, level_path)
        );

        CREATE TABLE placements (
            level_pk INTEGER NOT NULL,
            entity_ordinal INTEGER NOT NULL,
            record_pk INTEGER NOT NULL,
            world_x REAL NOT NULL,
            world_y REAL NOT NULL,
            world_z REAL NOT NULL,
            PRIMARY KEY (level_pk, entity_ordinal),
            FOREIGN KEY (level_pk) REFERENCES levels(id) ON DELETE CASCADE,
            FOREIGN KEY (record_pk) REFERENCES records(id)
        ) WITHOUT ROWID;
        """;

    public const string CreateIndexesSql = """
        CREATE INDEX items_filter_idx ON items(rarity, item_class, required_level);
        CREATE INDEX affixes_filter_idx ON affixes(rarity, kind, required_level);
        CREATE INDEX ascended_categories_filter_idx ON ascended_affix_categories(category, affix_pk);
        CREATE INDEX placements_record_idx ON placements(record_pk);
        """;

    public const string CreateReferenceIndexesSql = """
        CREATE INDEX references_target_idx ON record_references(target_pk);
        """;
}
