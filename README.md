# gd-cli

Agent-first CLI for querying Grim Dawn game data. Items are the primary domain: one item query can return the base record, set relations, game-defined variants, acquisition methods, actors, and coordinates. Affixes and quests are separate query domains.

The CLI writes compact JSON to stdout and JSON errors to stderr. It is designed for AI agents, not interactive browsing.

## AI agent setup

Run `gd-cli`.

Install or link `skills/gd-cli` into the agent's Skill directory. The Skill covers query selection, game terminology, coordinates, and compatibility-first BiS evaluation.

For Codex on Windows:

```powershell
New-Item -ItemType Junction `
    -Path "$HOME\.codex\skills\gd-cli" `
    -Target (Resolve-Path ".\skills\gd-cli")
```

## Database lifecycle

Run `gd-cli info` before querying.

| Result | Action |
|---|---|
| Success | Query normally. Do not run `init` unless the user requests a rebuild. |
| `database_not_found` | Run `init` when the game directory is known. |
| `incompatible_database` | Explain that `init` is required and wait for explicit user approval. |

Initialize with:

```text
gd-cli init <grim-dawn-game-directory> [--game-language en|zh]
```

`init` atomically rebuilds the CLI-owned database from game data. The default game-data language is `zh`; CLI syntax and field names remain English.

## Commands

```text
gd-cli tree
gd-cli init <grim-dawn-game-directory> [--game-language en|zh]
gd-cli info
gd-cli schema
gd-cli items [query] [filters] [paging]
gd-cli affixes [query] [filters] [paging]
gd-cli quests [query] [paging]
```

Use `gd-cli --help` for root commands and global flags. Use `gd-cli <command> --help` for command-specific arguments and options.

### Items

`items` is the single item query surface.

- Without `query`, it filters and pages item records.
- With a name or record ID, it returns matching items plus set relations, variants, acquisition methods, actors, routes, and coordinates.
- A set name returns its member items and the related set record.
- `--families` groups records by stable `nameTag` for MI and localization analysis.
- Lists omit `unavailable` records by default. An explicit query includes them. Use `--availability all` for catalog audits.

Availability is evidence-based:

- `known`: a supported acquisition or recipe path exists.
- `referenced`: live game, map, or quest data references the item.
- `unresolved`: imported evidence is insufficient.
- `unavailable`: explicit exclusion evidence exists without a live path.

Acquisition methods may include `vendor`, `specificMonster`, `randomDrop`, `craft`, and `unknown`. `unknown` means no supported source was derived; it does not prove the item is unavailable. Check `routesTruncated` before treating monster routes as complete.

### Affixes

`affixes` queries both `standard` Prefix and Suffix records and `ascended` affixes.

- `--family standard|ascended|all` selects the affix system.
- `--type <itemClass>` applies exact game item-class compatibility to standard affixes.
- `--category <category>` applies game-native equipment-category compatibility to Ascended affixes.
- A name or record ID returns exact matches first, then partial matches.

Valid values are reported by `info` and `schema`. See [BiS Affix Evaluation](skills/gd-cli/references/affix-ranking.md) for the agent workflow.

### Quests

Without a query, `quests` lists quest summaries. With a name or path, it returns the structured quest graph, branches, relevant actors, and available key coordinates. The database does not store full dialogue, Lua source, or precomputed routes.

## Output

- Global `--query JMESPATH` projects the complete JSON result.
- Query commands expose paging and `--no-stats` where supported. Use command help for their exact scope.
- Numeric values remain JSON numbers.
- Stable game terms and record IDs are preserved.

## Game data and safety

The repository directly parses Grim Dawn ARC, ARZ, text tags, maps, quests, conversations, and structured Lua quest metadata. `lz4net` decompresses LZ4 archive blocks. SQLite storage uses `Microsoft.Data.Sqlite`; `JmesPath.Net` implements `--query`.

Initialization opens game files read-only with shared access. It writes only the CLI-owned database, cleans up only its own temporary files, and never modifies game files.

## Build and test

Requires the .NET 10 SDK.

```powershell
.\build.ps1
.\test.ps1
```

## Release

Publishing a GitHub Release runs tests and uploads a self-contained single-file `win-x64` ZIP containing only the executable, README, and Skill files.

```powershell
git tag v1.0.0
git push origin v1.0.0
gh release create v1.0.0 --title "v1.0.0" --generate-notes
```
