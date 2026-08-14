---
name: gd-cli
description: Query Grim Dawn game data with gd-cli. Use for item acquisition, vendor, random drop, monster-specific loot, recipe, affix, Ascended affix, compatibility, numeric range, effect, quest graph, key map coordinate, schema, JMESPath, affix ranking, or BiS affix evaluation. Initialize automatically only when the CLI database is absent; never rebuild an existing database without an explicit user request.
---

# gd-cli

Run `gd-cli`.

Before a query, run `gd-cli info`:

- On `database_not_found`, run `init <grim-dawn-game-directory>` when the game directory is known.
- On `incompatible_database`, explain that `init` is required and wait for explicit user approval.
- On success, never run `init` unless the user explicitly requests a rebuild.

## Commands

| Command | Description |
|---|---|
| `tree` | Show the command tree. |
| `init <grim-dawn-game-directory>` | Rebuild the CLI database from game data. |
| `info` | Show database metadata and available values. |
| `schema` | Show fields, capabilities, and valid filter values. |
| `items` | Filter and list individual item records. |
| `item-families` | Group related records by stable game name tag. |
| `item <record-id>` | Get one complete item record and its stats. |
| `affixes` | Query prefix and suffix records, optionally by compatible item class. |
| `affix <record-id>` | Get one affix by exact record ID. |
| `ascended-affixes` | Query Ascended affixes by game-native equipment category. |
| `ascended-affix <record-id>` | Get one Ascended affix by exact record ID. |
| `acquisition <item-name-or-record-id>` | Find acquisition methods without item stats. |
| `quests` | Query quest definitions. |
| `quest <quest-name-or-path>` | Get a quest graph, relevant actors, and key coordinates. |
| `search <query>` | Search item and affix names or record IDs. |

## Global flags

- `--query JMESPATH`: Project the complete JSON result before stdout.

`gd-cli --help` returns root commands and global flags. `gd-cli <command-path> --help` returns that node's description, arguments, options, and direct subcommands when present.

## Principles

- Use the narrowest useful command. Discover capabilities through `tree`, command help, `info`, and `schema`.
- Treat CLI JSON as authoritative game data. Preserve canonical terms, numeric types, relationships, and uncertainty; combine or explain them as the task requires.
- Default to the initialized database language. Change source language only after an explicit request. Queries are read-only; `init` replaces only the CLI-owned database.

## Coordinate presentation

Present each complete teleport coordinate on one plain-text line:

`[<category>]<short label>, <x>, <y>, <z>`

Example: `[Dungeon]Ashen Waste, -967, 2.19, 3341`

Prefer a useful category, the canonical label, and the original `x`, `y`, `z` values. Put additional context outside the line.

For BiS affix evaluation, read [references/affix-ranking.md](references/affix-ranking.md).
