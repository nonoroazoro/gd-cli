namespace GdCli.Database;

internal static class DatabaseSchema
{
    public const int Version = 5;

    public static IReadOnlyList<string> RequiredTables { get; } = Array.AsReadOnly<string>(
    [
        "metadata",
        "tags",
        "records",
        "field_names",
        "record_fields",
        "record_references",
        "loot_conditions",
        "items",
        "item_sets",
        "item_set_members",
        "affixes",
        "affix_item_classes",
        "ascended_affix_categories",
        "item_variants",
        "affix_skill_modifiers",
        "acquisition_sources",
        "recipes",
        "levels",
        "placements",
        "quests",
        "quest_nodes",
        "quest_actions",
        "quest_conditions",
        "quest_edges",
        "quest_entities",
        "entity_aliases",
        "quest_unresolved_references"
    ]);

    public static readonly string CreateSql = $$"""
        PRAGMA page_size = 32768;
        PRAGMA foreign_keys = ON;
        PRAGMA user_version = {{Version}};

        CREATE TABLE metadata (
            key TEXT PRIMARY KEY,
            value TEXT NOT NULL
        ) WITHOUT ROWID;

        CREATE TABLE tags (
            tag TEXT PRIMARY KEY,
            text TEXT NOT NULL
        ) WITHOUT ROWID;

        CREATE TABLE records (
            id INTEGER PRIMARY KEY,
            record_id TEXT NOT NULL UNIQUE COLLATE NOCASE,
            class TEXT,
            template TEXT,
            name_tag TEXT,
            display_name TEXT
        );

        CREATE TABLE field_names (
            id INTEGER PRIMARY KEY,
            name TEXT NOT NULL UNIQUE
        );

        CREATE TABLE record_fields (
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

        CREATE TABLE loot_conditions (
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
            rarity TEXT NOT NULL,
            item_class TEXT NOT NULL,
            item_level REAL NOT NULL,
            required_level REAL NOT NULL,
            is_mi INTEGER NOT NULL DEFAULT 0,
            availability TEXT NOT NULL DEFAULT 'unresolved'
                CHECK (availability IN ('known', 'referenced', 'unresolved', 'unavailable')),
            FOREIGN KEY (record_pk) REFERENCES records(id) ON DELETE CASCADE
        );

        CREATE TABLE item_sets (
            record_pk INTEGER PRIMARY KEY,
            item_level REAL NOT NULL,
            availability TEXT NOT NULL DEFAULT 'unresolved'
                CHECK (availability IN ('known', 'referenced', 'unresolved', 'unavailable')),
            FOREIGN KEY (record_pk) REFERENCES records(id) ON DELETE CASCADE
        );

        CREATE TABLE item_set_members (
            set_pk INTEGER NOT NULL,
            item_pk INTEGER NOT NULL,
            ordinal INTEGER NOT NULL,
            PRIMARY KEY (set_pk, item_pk),
            UNIQUE (set_pk, ordinal),
            FOREIGN KEY (set_pk) REFERENCES item_sets(record_pk) ON DELETE CASCADE,
            FOREIGN KEY (item_pk) REFERENCES items(record_pk) ON DELETE CASCADE
        ) WITHOUT ROWID;

        CREATE TABLE affixes (
            record_pk INTEGER PRIMARY KEY,
            family TEXT NOT NULL CHECK (family IN ('standard', 'ascended', 'variant')),
            kind TEXT,
            rarity TEXT NOT NULL,
            item_level REAL NOT NULL,
            required_level REAL NOT NULL,
            jitter_percent REAL NOT NULL,
            CHECK ((family = 'ascended' AND kind IS NULL) OR
                   (family IN ('standard', 'variant') AND kind IN ('prefix', 'suffix'))),
            FOREIGN KEY (record_pk) REFERENCES records(id) ON DELETE CASCADE
        );

        CREATE TABLE affix_item_classes (
            item_class TEXT NOT NULL COLLATE NOCASE,
            affix_pk INTEGER NOT NULL,
            PRIMARY KEY (item_class, affix_pk),
            FOREIGN KEY (affix_pk) REFERENCES affixes(record_pk) ON DELETE CASCADE
        ) WITHOUT ROWID;

        CREATE TABLE ascended_affix_categories (
            affix_pk INTEGER NOT NULL,
            category TEXT NOT NULL COLLATE NOCASE,
            group_name TEXT NOT NULL COLLATE NOCASE,
            PRIMARY KEY (affix_pk, category, group_name),
            FOREIGN KEY (affix_pk) REFERENCES affixes(record_pk) ON DELETE CASCADE
        ) WITHOUT ROWID;

        CREATE TABLE item_variants (
            item_pk INTEGER NOT NULL,
            affix_pk INTEGER NOT NULL,
            source_pk INTEGER NOT NULL,
            PRIMARY KEY (item_pk, affix_pk, source_pk),
            FOREIGN KEY (item_pk) REFERENCES items(record_pk) ON DELETE CASCADE,
            FOREIGN KEY (affix_pk) REFERENCES affixes(record_pk) ON DELETE CASCADE,
            FOREIGN KEY (source_pk) REFERENCES records(id)
        ) WITHOUT ROWID;

        CREATE TABLE affix_skill_modifiers (
            affix_pk INTEGER NOT NULL,
            modifier_pk INTEGER NOT NULL,
            ordinal INTEGER NOT NULL,
            skill_pk INTEGER,
            PRIMARY KEY (affix_pk, ordinal),
            FOREIGN KEY (affix_pk) REFERENCES affixes(record_pk) ON DELETE CASCADE,
            FOREIGN KEY (modifier_pk) REFERENCES records(id),
            FOREIGN KEY (skill_pk) REFERENCES records(id)
        ) WITHOUT ROWID;

        CREATE TABLE acquisition_sources (
            item_pk INTEGER NOT NULL,
            kind TEXT NOT NULL,
            source_pk INTEGER,
            CHECK (kind IN ('specificMonster', 'vendor', 'randomDrop')),
            CHECK ((kind = 'randomDrop' AND source_pk IS NULL) OR
                   (kind <> 'randomDrop' AND source_pk IS NOT NULL)),
            FOREIGN KEY (item_pk) REFERENCES items(record_pk) ON DELETE CASCADE,
            FOREIGN KEY (source_pk) REFERENCES records(id)
        );

        CREATE TABLE recipes (
            result_item_pk INTEGER NOT NULL,
            recipe_item_pk INTEGER NOT NULL,
            PRIMARY KEY (result_item_pk, recipe_item_pk),
            FOREIGN KEY (result_item_pk) REFERENCES items(record_pk) ON DELETE CASCADE,
            FOREIGN KEY (recipe_item_pk) REFERENCES items(record_pk) ON DELETE CASCADE
        ) WITHOUT ROWID;

        CREATE TABLE levels (
            id INTEGER PRIMARY KEY,
            source_name TEXT NOT NULL,
            level_path TEXT NOT NULL COLLATE NOCASE,
            rift_gate_record_id TEXT NOT NULL COLLATE NOCASE,
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

        CREATE TABLE quests (
            id INTEGER PRIMARY KEY,
            quest_path TEXT NOT NULL UNIQUE COLLATE NOCASE,
            source_name TEXT NOT NULL,
            uid INTEGER NOT NULL,
            flags INTEGER NOT NULL,
            region TEXT NOT NULL,
            name TEXT NOT NULL
        );

        CREATE TABLE quest_nodes (
            id INTEGER PRIMARY KEY,
            quest_pk INTEGER NOT NULL,
            parent_pk INTEGER,
            ordinal INTEGER NOT NULL,
            kind TEXT NOT NULL,
            phase TEXT NOT NULL,
            uid INTEGER,
            link_id INTEGER,
            is_blocker INTEGER,
            dont_propagate INTEGER,
            name TEXT NOT NULL,
            description TEXT NOT NULL,
            flags INTEGER NOT NULL,
            condition_operator TEXT NOT NULL,
            origin_path TEXT NOT NULL,
            FOREIGN KEY (quest_pk) REFERENCES quests(id) ON DELETE CASCADE,
            FOREIGN KEY (parent_pk) REFERENCES quest_nodes(id) ON DELETE CASCADE
        );

        CREATE TABLE quest_actions (
            node_pk INTEGER NOT NULL,
            ordinal INTEGER NOT NULL,
            kind TEXT NOT NULL,
            quest_path TEXT,
            task_uid INTEGER,
            objective_uid INTEGER,
            record_id TEXT,
            token TEXT,
            function_name TEXT,
            text_value TEXT,
            numeric_value REAL,
            secondary_numeric_value REAL,
            tertiary_numeric_value REAL,
            boolean_value INTEGER,
            PRIMARY KEY (node_pk, ordinal),
            FOREIGN KEY (node_pk) REFERENCES quest_nodes(id) ON DELETE CASCADE
        ) WITHOUT ROWID;

        CREATE TABLE quest_conditions (
            node_pk INTEGER NOT NULL,
            ordinal INTEGER NOT NULL,
            kind TEXT NOT NULL,
            comparison INTEGER,
            quest_path TEXT,
            task_uid INTEGER,
            objective_uid INTEGER,
            record_id TEXT,
            token TEXT,
            function_name TEXT,
            text_value TEXT,
            numeric_value REAL,
            secondary_numeric_value REAL,
            tertiary_numeric_value REAL,
            boolean_value INTEGER,
            PRIMARY KEY (node_pk, ordinal),
            FOREIGN KEY (node_pk) REFERENCES quest_nodes(id) ON DELETE CASCADE
        ) WITHOUT ROWID;

        CREATE TABLE quest_edges (
            id INTEGER PRIMARY KEY,
            quest_pk INTEGER NOT NULL,
            source_node_pk INTEGER NOT NULL,
            target_quest_path TEXT NOT NULL COLLATE NOCASE,
            target_task_uid INTEGER,
            kind TEXT NOT NULL,
            origin_path TEXT NOT NULL,
            FOREIGN KEY (quest_pk) REFERENCES quests(id) ON DELETE CASCADE,
            FOREIGN KEY (source_node_pk) REFERENCES quest_nodes(id) ON DELETE CASCADE
        );

        CREATE TABLE quest_entities (
            id INTEGER PRIMARY KEY,
            quest_pk INTEGER NOT NULL,
            node_pk INTEGER,
            record_pk INTEGER NOT NULL,
            role TEXT NOT NULL,
            origin_path TEXT NOT NULL,
            UNIQUE (quest_pk, node_pk, record_pk, role, origin_path),
            FOREIGN KEY (quest_pk) REFERENCES quests(id) ON DELETE CASCADE,
            FOREIGN KEY (node_pk) REFERENCES quest_nodes(id) ON DELETE CASCADE,
            FOREIGN KEY (record_pk) REFERENCES records(id)
        );

        CREATE TABLE entity_aliases (
            alias_pk INTEGER NOT NULL,
            placed_pk INTEGER NOT NULL,
            origin_path TEXT NOT NULL,
            PRIMARY KEY (alias_pk, placed_pk, origin_path),
            FOREIGN KEY (alias_pk) REFERENCES records(id),
            FOREIGN KEY (placed_pk) REFERENCES records(id)
        ) WITHOUT ROWID;

        CREATE TABLE quest_unresolved_references (
            id INTEGER PRIMARY KEY,
            quest_pk INTEGER NOT NULL,
            node_pk INTEGER,
            kind TEXT NOT NULL,
            value TEXT NOT NULL,
            origin_path TEXT NOT NULL,
            UNIQUE (quest_pk, node_pk, kind, value, origin_path),
            FOREIGN KEY (quest_pk) REFERENCES quests(id) ON DELETE CASCADE,
            FOREIGN KEY (node_pk) REFERENCES quest_nodes(id) ON DELETE CASCADE
        );
        """;

    public const string CreateIndexesSql = """
        CREATE INDEX items_filter_idx ON items(availability, rarity, item_class, required_level);
        CREATE INDEX item_set_members_item_idx ON item_set_members(item_pk, set_pk);
        CREATE INDEX affixes_filter_idx ON affixes(family, rarity, kind, required_level);
        CREATE INDEX ascended_categories_filter_idx ON ascended_affix_categories(category, affix_pk);
        CREATE INDEX placements_record_idx ON placements(record_pk);
        CREATE INDEX quests_name_idx ON quests(name COLLATE NOCASE);
        CREATE INDEX quest_nodes_quest_idx ON quest_nodes(quest_pk, ordinal);
        CREATE INDEX quest_edges_quest_idx ON quest_edges(quest_pk, source_node_pk);
        CREATE INDEX quest_entities_quest_idx ON quest_entities(quest_pk, node_pk);
        CREATE INDEX quest_unresolved_quest_idx ON quest_unresolved_references(quest_pk, node_pk);
        CREATE INDEX entity_aliases_placed_idx ON entity_aliases(placed_pk);
        """;

    public const string CreateBuildIndexesSql = """
        CREATE INDEX references_target_idx ON record_references(target_pk);
        CREATE UNIQUE INDEX acquisition_sources_unique_idx
            ON acquisition_sources(item_pk, kind, COALESCE(source_pk, 0));
        """;
}
