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

## Contract

- Results are compact JSON on stdout. Errors are JSON on stderr. Exit codes indicate status.
- Numeric fields are JSON numbers.
- Locate with `search`, `items`, or `item-families`; inspect stats with `item`; query sources with `acquisition`.
- Use `items --mi true` for MI records and `item-families --mi true` for families containing an MI record. On `item-families`, `--mi false` means no MI records.
- Use `affixes --type <itemClass>` for compatible prefix and suffix records.
- Use `ascended-affixes --category <category>` for Ascended affixes. Read valid raw values from `info` or `schema`.
- Keep normal and Ascended affix results separate. Combine them only in agent reasoning.
- For BiS evaluation, default to sustained real-combat performance unless the user explicitly requests burst, one-shot, or single-hit optimization. Model the complete rotation, filler actions, WPS, and trigger frequency. Do not rank only the main skill's single hit.
- Treat acquisition methods as additive. An item may have `vendor`, `specificMonster`, `randomDrop`, and `craft` methods.
- For `craft`, inspect the recipe and its nested known sources.
- `randomDrop` represents a randomized loot pool and intentionally does not enumerate every monster using that pool.
- Check `routesTruncated` before treating `specificMonster` routes as complete.
- Treat `quest.nodes` and `quest.edges` as a graph. Preserve branches, use entity coordinates only when present, and report unresolved references instead of guessing.
- CLI-owned text is English. Parsed names use the language selected by `init`.
- Queries read only the CLI database. `init` opens game files read-only with shared access, cleans up only its own temporary database, and never modifies game files.
- Use `<command-path> --help` for node-specific help. Use `tree` and `schema` for discovery and raw values.

For BiS affix evaluation, read [references/affix-ranking.md](references/affix-ranking.md).
