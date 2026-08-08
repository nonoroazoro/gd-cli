namespace GdCli.Database;

internal static class DatabaseSchema
{
    public const int Version = 1;

    public const string CreateSql = """
        PRAGMA page_size = 32768;
        PRAGMA foreign_keys = ON;
        PRAGMA user_version = 1;

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
        CREATE INDEX placements_record_idx ON placements(record_pk);
        """;
}
