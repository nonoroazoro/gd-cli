# gd-cli

Agent-first CLI for querying Grim Dawn items, affixes, acquisition methods, quests, and key map coordinates. It returns stable, compact JSON and is not designed for interactive human use.

## AI agent setup

Run `gd-cli`.

Install or link `skills/gd-cli` into the agent's Skill directory.

The Skill covers acquisition queries and compatibility-first BiS evaluation for Prefix, Suffix, and Ascended affixes.

For Codex on Windows:

```powershell
New-Item -ItemType Junction `
    -Path "$HOME\.codex\skills\gd-cli" `
    -Target (Resolve-Path ".\skills\gd-cli")
```

### Database lifecycle

Run `gd-cli info` before querying.

| Result | Action |
|---|---|
| Success | Query normally. Do not run `init` unless the user requests a rebuild. |
| `database_not_found` | Run `init` when the game directory is known. |
| `incompatible_database` | Explain that `init` is required and wait for explicit user approval. |

The CLI never rebuilds an existing database automatically.

## Initialize

```text
gd-cli init <grim-dawn-game-directory> [--game-language en|zh]
```

`init` atomically builds the database from game data. The default game-data language is `zh`. CLI syntax and fields remain English.

## Commands

```text
gd-cli tree
gd-cli init <grim-dawn-game-directory> [--game-language en|zh]
gd-cli info
gd-cli schema
gd-cli items [filters] [paging]
gd-cli item-families [--mi true|false] [paging]
gd-cli item <record-id> [--no-stats]
gd-cli affixes [filters] [paging]
gd-cli affix <record-id> [--no-stats]
gd-cli ascended-affixes [filters] [paging]
gd-cli ascended-affix <record-id> [--no-stats]
gd-cli acquisition <item-name-or-record-id> [paging]
gd-cli quests [paging]
gd-cli quest <quest-name-or-path> [paging]
gd-cli search <query> [filters] [paging]
```

Use `gd-cli --help` for root commands and global flags. Use `gd-cli <command> --help` for command arguments and options.

### Item queries

| Need | Command |
|---|---|
| Find or filter individual records | `search`, `items` |
| Group related records by `nameTag` | `item-families` |
| Read one complete record and its stats | `item <record-id>` |
| Find acquisition methods | `acquisition <name-or-record-id>` |

Locate the item, inspect its record, then query acquisition as needed.

Common options:

- `--query JMESPATH` projects the final JSON.
- `--offset`, `--limit`, and `--all` control paging.
- `--no-stats` skips item details or affix range calculation.
- Filters execute in SQLite when supported by command help.

## Output contract

- stdout contains compact JSON results.
- stderr contains JSON errors.
- Exit codes indicate status.
- Numeric fields are JSON numbers.
- Affix `effects` include chance effects and skill bonuses; `skillBonuses` preserves stable skill record IDs and numeric levels.

### MI families

`nameTag` groups localized item records. `item-families` preserves mixed MI and non-MI state.

- `--mi true` selects families containing an MI record.
- `--mi false` selects families containing no MI records.
- `info.miCount` is a compatibility alias of `info.miRecordCount`.

### Acquisition

`acquisition` reports every known way to obtain an item:

- `vendor`: direct merchant inventory, with known map coordinates.
- `specificMonster`: dedicated monster loot paths, with actors, conditions, and coordinates.
- `randomDrop`: randomized loot pools without expanding every monster using the pool.
- `craft`: a recipe or design plus its known acquisition sources.
- `unknown`: no supported source was derived from the game data.

Methods are not mutually exclusive. Check `routesTruncated` before treating `specificMonster` routes as complete.

### Affix compatibility

`affixes --type <itemClass>` returns prefix and suffix records supported by that exact game item class.

Ascended affixes use the game's broader equipment categories. Query them separately with `ascended-affixes --category <category>`. Valid categories are reported by `info` and `schema`.

See [BiS Affix Evaluation](skills/gd-cli/references/affix-ranking.md) for the agent workflow.

### Quests

`quest` returns task, objective, event, conversation, and script nodes as a graph. Relevant actors and targets include fixed map coordinates when available. Branches remain explicit, and unresolved references are reported instead of guessed.

The database stores structured quest metadata only. It does not store full dialogue, Lua source, or precomputed routes.

## Game data and safety

The repository directly parses Grim Dawn ARC, ARZ, tags, level maps, quests, conversations, and structured Lua quest metadata. `lz4net` decompresses LZ4 archive blocks. SQLite storage uses `Microsoft.Data.Sqlite`; `JmesPath.Net` implements `--query`.

Initialization opens game files read-only with shared access. It writes only the CLI-owned database, cleans up only its own temporary files, and never modifies game files.

## Build and test

Requires the .NET 10 SDK.

```powershell
.\build.ps1
.\test.ps1
```

## Release

Publishing a GitHub Release runs tests and uploads a self-contained single-file `win-x64` ZIP. The ZIP contains only the executable, README, and Skill files.

Create and publish a GitHub Release, or use:

```powershell
git tag v1.0.0
git push origin v1.0.0
gh release create v1.0.0 --title "v1.0.0" --generate-notes
```
