---
name: gd-cli
description: Query Grim Dawn game data with gd-cli. Use for item or affix lookup, numeric affix ranges, English effect summaries, monster-specific drops, map coordinates, schema discovery, JMESPath filtering, or affix ranking. Initialize automatically only when the CLI database is absent; never rebuild an existing database without an explicit user request.
---

# gd-cli

Run `.\gd-cli.exe` from the workspace root. It reads `data/gd-cli.db` beside the executable.

Before a query, check `data/gd-cli.db`. Run `init <grim-dawn-game-directory>` automatically only when the database is absent. If it exists, never run `init` unless the user explicitly requests a rebuild.

## Commands

| Command | Description |
|---|---|
| `tree` | Show the command tree. |
| `init <grim-dawn-game-directory>` | Rebuild the CLI database from game data. |
| `info` | Show database metadata and available values. |
| `schema` | Show fields, capabilities, and valid filter values. |
| `items` | Query item records. |
| `item <record-id>` | Get one item by exact record ID. |
| `affixes` | Query prefix and suffix records. |
| `affix <record-id>` | Get one affix by exact record ID. |
| `drops <item-name-or-record-id>` | Find monster-specific item drops and map locations. |
| `search <query>` | Search item and affix names or record IDs. |

## Global flags

- `--query JMESPATH`: Project the complete JSON result before stdout.

`gd-cli --help` returns root commands and global flags. `gd-cli <command-path> --help` returns that node's description, arguments, options, and direct subcommands when present.

## Contract

- Results are compact JSON on stdout. Errors are JSON on stderr. Exit codes indicate status.
- Numeric fields are JSON numbers.
- CLI-owned text is English. Parsed names use the language selected by `init`.
- Queries read only the CLI database. `init` opens game files read-only with shared access, cleans up only its own temporary database, and never modifies game files.
- Use `<command-path> --help` for node-specific help. Use `tree` and `schema` for discovery and raw values.

For affix ranking, read [references/affix-ranking.md](references/affix-ranking.md).
