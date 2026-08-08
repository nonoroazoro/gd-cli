# gd-cli

Agent-first CLI for querying Grim Dawn items, affixes, monster-specific drops, and map coordinates. It is designed for AI coding agents, not interactive human use. Commands return stable, compact JSON for agent discovery, filtering, and composition.

## AI agent setup

Extract the release and use its directory as the agent workspace. Install or link `skills/gd-cli` into the agent's Skill directory so the bundled instructions stay aligned with the executable.

For Codex on Windows:

```powershell
New-Item -ItemType Junction `
    -Path "$HOME\.codex\skills\gd-cli" `
    -Target (Resolve-Path ".\skills\gd-cli")
```

Then ask the agent to initialize or query Grim Dawn data with `gd-cli`. The Skill requires `init` only when `data/gd-cli.db` is absent and prevents an agent from rebuilding an existing database unless explicitly requested.

## Game data parsing

The repository directly parses Grim Dawn ARC, ARZ, tags, and level map files. `lz4net` only decompresses LZ4 blocks used by game archives. SQLite storage uses `Microsoft.Data.Sqlite`; `JmesPath.Net` implements `--query`.

Initialization opens game files read-only with shared read, write, and delete access. It writes only the CLI-owned database, cleans up only temporary files created by the current initialization, and never modifies game files.

## Build and test

Requires the .NET 10 SDK on `PATH`.

```powershell
.\build.ps1
.\test.ps1
```

Build output is under `bin/Release/net10.0/win-x64`. The database is `data/gd-cli.db` beside `gd-cli.exe`.

## Release

Publishing a GitHub Release runs tests, publishes a self-contained single-file `win-x64` build from the release tag, verifies that the published executable starts, and uploads `gd-cli-<tag>-win-x64.zip` to that Release. Users can extract and run `gd-cli.exe` without installing .NET. The ZIP contains only `gd-cli.exe`, README, and Skill files. A release allowlist rejects runtime files, symbols, the CLI database, and any other unexpected output.

To trigger publishing from GitHub, open **Releases**, create a release for the target commit, and select **Publish release**. Saving a draft does not trigger the workflow.

To trigger publishing with GitHub CLI:

```powershell
git tag v1.0.0
git push origin v1.0.0
gh release create v1.0.0 --title "v1.0.0" --generate-notes
```

The `Release` workflow builds from that tag and attaches `gd-cli-v1.0.0-win-x64.zip` to the published release.

## Initialize

```text
gd-cli init <grim-dawn-game-directory> [--game-language en|zh]
```

Use `init` to create the database or deliberately rebuild it. The default game-data language is `zh`. A successful `init` atomically replaces the single database. CLI syntax, fields, errors, and effect summaries remain English; parsed names use the selected game-data language.

## Commands

```text
gd-cli tree
gd-cli init <grim-dawn-game-directory> [--game-language en|zh]
gd-cli info
gd-cli schema
gd-cli items [filters] [paging]
gd-cli item <record-id> [--no-stats]
gd-cli affixes [filters] [paging]
gd-cli affix <record-id> [--no-stats]
gd-cli drops <item-name-or-record-id> [paging]
gd-cli search <query> [filters] [paging]
```

`gd-cli --help` returns root commands and global flags. `<command-path> --help` returns that node's description, arguments, options, and direct subcommands when present. Available filters are `--rarity`, `--type`, `--kind`, `--min-level`, and `--max-level`. Paging uses `--offset`, `--limit`, or `--all`. Use `tree` and `schema` for discovery and valid raw values.

`--query JMESPATH` projects the final JSON. `--no-stats` skips item source details or affix range calculation. List filters, counts, exact lookups, searches, and paging execute in SQLite.

```text
gd-cli items --type WeaponMelee_Mace --rarity Rare --limit 20
gd-cli affixes --kind prefix --rarity Rare --no-stats --limit 25
gd-cli search "Gargabol" --limit 10
gd-cli drops "Gargabol's Ring" --all
gd-cli affixes --kind suffix --all --query "data[].{id:recordId,n:name,e:effects}"
```

stdout contains JSON results. stderr contains JSON errors. Exit codes indicate status. Plain stderr is used only if an error itself cannot be serialized.

`drops` covers equipment with monster-specific drop relations, including Rare, Epic, and Legendary records. Random world drops are excluded. An empty `routes` array means no fixed map placement was found.

Item-to-affix compatibility and Ascended affixes are not modeled.
