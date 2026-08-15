# Core Design

- Treat `item` as the primary RPG domain. Item queries aggregate identity, set relations, variants, availability, acquisition, source entities, routes, and coordinates.
- Keep root query domains non-overlapping: `items`, `affixes`, and `quests`. Do not add separate commands for data already owned by one domain.
- Keep the SQLite model normalized and independent from command shape. Store stable entities and relations once; compose agent-facing JSON in the application layer.
- Optimize filters and joins in SQLite. Avoid loading a full catalog for application-side filtering.
- Parse game metadata without custom taxonomies. Preserve record IDs, game terms, numeric types, relations, and uncertainty.
- Treat standard, Ascended, and item-specific variant affixes as distinct systems while reusing shared storage and stat processing where structurally valid.
- Keep queries read-only. `init` may replace only the CLI-owned database and must never modify or lock game files beyond an active read.
- Prefer a clean schema redesign over compatibility patches. An incompatible database requires an explicit `init` rebuild.
- Keep stdout as stable compact JSON, stderr as JSON diagnostics, and exit codes meaningful for agents.
- Remove obsolete commands, code, schema elements, docs, and tests when a design is replaced.
