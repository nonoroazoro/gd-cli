---
name: gd-cli
description: Query Grim Dawn items, MI status, sets, variants, availability, acquisition, vendors, monsters, recipes, affixes, Ascended affixes, compatibility, quests, and coordinates with gd-cli. Use for game-data research, affix ranking, or BiS evaluation. Initialize automatically only when the CLI database is absent; never rebuild an existing database without explicit user approval.
---

# gd-cli

## Preserve game data verbatim

- Follow the user's language for prose.
- Use the user's wording only to resolve intent and construct queries.
- Never translate, rename, or localize data returned by `gd-cli`. Preserve returned values exactly as emitted, even when they differ from the conversation language. This is a hard constraint: do not substitute synonyms, aliases, equivalents from another language, or inferred names.

Example: if the user asks about `Leap` and `gd-cli` returns the skill name `跃击`, write `跃击` in the answer. Write `Leap` only when `gd-cli` returns `Leap`.

Run `gd-cli info` once before the first query of a session. Reuse that result for later queries; do not run `info` before every command.

- On `database_not_found`, run `gd-cli init <grim-dawn-game-directory>` when the directory is known.
- On `incompatible_database`, explain that `init` is required and wait for explicit user approval.
- On success, never run `init` unless the user explicitly requests a rebuild.

## Commands

| Command | Description |
|---|---|
| `tree` | Show the command tree. |
| `init <grim-dawn-game-directory> [--game-language en\|zh]` | Rebuild the CLI database from game data. The default game-data language is `zh`. |
| `info` | Show database metadata and valid values. |
| `schema` | Show fields and capabilities. |
| `items [query] [filters] [paging]` | List, filter, or inspect items by rarity, type, level, MI status, or availability. A specific item includes stats, skill modifiers, tiered set bonuses, variants, availability, acquisition, source entities, routes, and coordinates. |
| `affixes [query] [filters] [paging]` | Query standard Prefix and Suffix records or Ascended affixes, including effects and item-type compatibility. |
| `quests [query] [paging]` | List quests or inspect a quest graph with branches, actors, and available key coordinates. |

## Global flags

- `--query JMESPATH`: Project the complete JSON result.

Use root or command help for current arguments and filters.

## Principles

- Treat items as the primary domain. Use `items <name-or-record-id>` for identity, modifiers, variants, set bonuses, and acquisition instead of composing overlapping lookups.
- Preserve numeric values, record IDs, relationships, and uncertainty.
- Treat `container` as a specific fixed-container source; generic world-loot chests remain `randomDrop`.
- `unknown` acquisition and `unresolved` availability do not mean unavailable.
- Queries are read-only. `init` replaces only the CLI-owned database.

Present complete teleport coordinates as one plain-text line:

`[<category>]<short label>, <x>, <y>, <z>`

For BiS affix evaluation, read [references/affix-ranking.md](references/affix-ranking.md).
