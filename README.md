# gd-cli

Agent-first CLI for querying Grim Dawn items, affixes, monster-specific drops, and map coordinates. It returns stable, compact JSON and is not designed for interactive human use.

## AI agent setup

Run `gd-cli`.

Install or link `skills/gd-cli` into the agent's Skill directory.

The Skill includes a compatibility-first workflow for evaluating BiS Prefix, Suffix, and Ascended affixes.

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
gd-cli drops <item-name-or-record-id> [paging]
gd-cli search <query> [filters] [paging]
```

Use `gd-cli --help` for root commands and global flags. Use `gd-cli <command> --help` for command arguments and options.

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

### MI families

`nameTag` groups localized item records. `item-families` preserves mixed MI and non-MI state.

- `--mi true` selects families containing an MI record.
- `--mi false` selects families containing no MI records.
- `info.miCount` is a compatibility alias of `info.miRecordCount`.

### Drops

`drops` covers Rare, Epic, and Legendary equipment with monster-specific drop relations. Random world drops are excluded.

- An empty `routes` array means no fixed map placement was found.
- Check `routesTruncated` before treating routes as complete.

### Affix compatibility

`affixes --type <itemClass>` returns prefix and suffix records supported by that exact game item class.

Ascended affixes use the game's broader equipment categories. Query them separately with `ascended-affixes --category <category>`. Valid categories are reported by `info` and `schema`.

See [BiS Affix Evaluation](skills/gd-cli/references/affix-ranking.md) for the agent workflow.

## Game data and safety

The repository directly parses Grim Dawn ARC, ARZ, tags, and level map files. `lz4net` decompresses LZ4 archive blocks. SQLite storage uses `Microsoft.Data.Sqlite`; `JmesPath.Net` implements `--query`.

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
