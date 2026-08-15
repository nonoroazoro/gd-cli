# BiS Affix Evaluation

Identify the best type-compatible Prefix, Suffix, and Ascended affixes for a specific equipment type and build.

Resistance Reduction is a core offensive dependency. Audit relevant RR candidates and the build's existing RR baseline when available; reliable, non-redundant RR often outweighs ordinary damage bonuses. Missing RR context must not block ranking: state a baseline assumption, keep the result conditional, and identify the existing RR that would reverse it.

## Required context

- Exact `itemClass` for Prefix and Suffix compatibility.
- Exact base item or record ID when game-defined variants may exist.
- Main skill, damage type, and attack or cast style.
- Filler actions used during cooldowns and effects triggered by those actions.
- Existing RR for each post-conversion damage type when available, separated by wording category, source, value, trigger, duration, and uptime.
- Game-native Ascended category when Ascended ranking is requested.

Mastery, base item, existing conversion, OA, target DA, speed caps, cooldowns, defenses, and the rest of the build improve accuracy. Do not require every detail before producing a useful ranking. Infer the most plausible rotation from the stated main skill, base item skill support, mastery skills, and candidate `skillBonuses`; state those assumptions. Return conditional candidates instead of a definitive BiS result when an unknown value can reverse the ranking.

## Default objective

When the user asks for the best or BiS affixes without naming an objective, optimize sustained real-combat performance over repeated cooldown cycles.

- Use the main skill whenever available.
- Fill every cooldown gap with the most plausible supported attack or cast.
- Include WPS, DoT uptime, buffs, debuffs, sustain, and per-action triggers used by that filler.
- Treat single-hit burst as a secondary result. Make it primary only when the user explicitly asks for burst, one-shot, or single-hit damage.
- Treat sheet damage as supporting evidence, not the objective.
- Value defense when it materially preserves attacking uptime or prevents death. Otherwise use it as a tie-breaker.

If the filler is unknown, infer candidates from the base item's skill bonuses and modifiers, then give a sustained recommendation for the most likely rotation plus a reversal condition for another plausible filler. Never silently model cooldown gaps as idle time.

When a cooldown main skill is paired with base-item support for a WPS, treat a WPS-capable default attack or default attack replacer as the likely filler. Inspect candidate affixes for ranks to that filler before ranking. A user calling the cooldown skill the main skill does not imply that filler damage is irrelevant.

## Candidate discovery

Resolve raw values from `info` or `schema`. Never infer compatibility from affix names.

```powershell
gd-cli affixes --family standard --type <itemClass> --kind prefix --all --query "data[].{recordId:recordId,name:name,rarity:rarity,itemLevel:itemLevel,requiredLevel:requiredLevel,stats:stats,effects:effects,skillBonuses:skillBonuses,unmodeledFields:unmodeledFields}"
gd-cli affixes --family standard --type <itemClass> --kind suffix --all --query "data[].{recordId:recordId,name:name,rarity:rarity,itemLevel:itemLevel,requiredLevel:requiredLevel,stats:stats,effects:effects,skillBonuses:skillBonuses,unmodeledFields:unmodeledFields}"
gd-cli affixes --family ascended --category <category> --all --query "data[].{recordId:recordId,name:name,groups:groups,stats:stats,effects:effects,skillBonuses:skillBonuses,skillModifiers:skillModifiers,unmodeledFields:unmodeledFields}"
gd-cli items <item-name-or-record-id> --all --query "data[].{recordId:recordId,name:name,stats:stats,variants:variants}"
```

Apply `--rarity`, `--min-level`, and `--max-level` when the request supplies those constraints. Pass an affix record ID to `affixes` for exact details.

Prefix, Suffix, and Ascended are independent systems:

- Evaluate Prefix candidates against Prefix candidates.
- Evaluate Suffix candidates against Suffix candidates.
- Rank Ascended candidates within the requested category and preserve their `groups` value.
- Evaluate complete Prefix plus Suffix combinations before declaring normal-affix BiS.
- Do not substitute Ascended for Prefix or Suffix.

Game-defined variants are base-item-specific components, not members of the standard affix pool. Evaluate their direct stats and `skillModifiers` before ranking the item. Preserve `kind` and `sourceRecordIds`; do not assume combinations that the returned relations do not establish.

## BiS evaluation

Use raw numeric `stats` for reasoning. Use `effects` to interpret mechanics, not to replace localized names or other returned game data in the answer.

1. Inspect the base item before ranking. Record every supported main skill, filler, WPS, modifier, conversion, and proc that can reveal the intended rotation.
2. Build one complete repeated cycle: main-skill casts, cooldown gaps, filler actions, WPS, buffs, debuffs, DoT applications, and relevant triggers.
3. Use sustained contribution per cycle as the primary comparison. Conceptually compare `(main-skill contribution + filler contribution + expected proc contribution + maintained DoT contribution) / cycle time`.
4. Remove stats unrelated to that rotation, damage type, or objective. Mark unrelated skill ranks, speed types, damage types, and capped stats as dead value.
5. Merge candidate stats with the base item and known build state. Marginal value determines BiS, not isolated magnitude.
6. Read `skillBonuses` before ranking. Weight a filler or WPS bonus by how often it contributes during the complete cycle, not by whether it affects the named main skill.
7. Compare complete Prefix plus Suffix packages. For close combinations, cancel shared stats first and compare only their differing marginal contributions.
8. Compare `minimum` and `maximum`; treat equal values as fixed.
9. For chance-based damage, use `chance / 100 * average(minimum, maximum)` per eligible action while retaining the original chance and range. Multiply the expected value by eligible actions per cycle.
10. For per-action triggers, estimate trigger rate from eligible actions per second. Value speed when it increases main-skill execution or filler, WPS, and trigger counts and remains below cap.
11. Do not discard Attack Speed or Cast Speed merely because the main skill has a cooldown. Apply it to actions performed during the cooldown gap.
12. Value OA through hit and crit frequency against target DA. Value Crit Damage only when OA permits critical hits. Do not call either universally superior without the OA and target DA context.
13. Value deterministic cooldown reduction only for cooldown-limited skills. Evaluate chance-based cooldown effects from their trigger and action rate, never as equivalent global cooldown reduction.
14. Evaluate conversion and resistance reduction only with their direction, damage type, duration, trigger, and stacking context.
15. Account for WPS eligibility and pool weight. A WPS rank is dead value when the filler cannot trigger WPS or when the WPS is intentionally excluded.
16. Treat opaque procs and granted skills as unknown unless returned fields expose their effect.
17. Inspect `unmodeledFields`; report uncertainty when a relevant field is not modeled.
18. Do not claim exact DPS without the base item and full build. Prefer dominance, expected-value, and reversal-condition reasoning over invented precision.

### Cooldown rotation template

For a cooldown main skill, use this sequence:

1. Determine casts per cycle from cooldown and deterministic cooldown reduction.
2. Estimate usable filler actions in the remaining time from animation time and effective speed.
3. Apply filler skill ranks, WPS eligibility, and per-action proc expectations to those actions.
4. Add main-skill and filler contributions, then compare candidates over the same cycle length.
5. Report a separate burst winner only when it differs from the sustained winner.

If exact timings are unavailable, compare directionally:

- More filler ranks, WPS ranks, and speed gain value as filler uptime increases.
- More OA and Crit Damage value as target DA rises from trivial to crit-relevant ranges, then depend on actual OA breakpoints.
- Chance damage value equals its expectation across all eligible main and filler actions, not only the main-skill hit.
- A bonus to an unused skill has zero offensive value even when the rest of the affix is strong.

### Damage fields

| Damage | Direct fields | DoT fields |
|---|---|---|
| Physical | `offensiveBonusPhysical*`, `offensivePhysicalModifier` | `offensiveSlowPhysical*` |
| Pierce | `offensivePierce*` | none |
| Fire | `offensiveFire*` | `offensiveSlowFire*` |
| Cold | `offensiveCold*` | `offensiveSlowCold*` |
| Lightning | `offensiveLightning*` | `offensiveSlowLightning*` |
| Acid | `offensivePoison*`, excluding slow fields | `offensiveSlowPoison*` |
| Vitality | `offensiveLife*`, excluding slow fields | `offensiveSlowLife*` |
| Aether | `offensiveAether*` | none |
| Chaos | `offensiveChaos*` | none |
| Elemental | `offensiveElemental*` | none |
| Bleeding | none | `offensiveSlowBleeding*` |

Bleeding is independent from Pierce. Legacy fields map `characterStrength` to Physique, `characterDexterity` to Cunning, and `characterIntelligence` to Spirit.

### High-impact supporting fields

- OA: `characterOffensiveAbility`, `characterOffensiveAbilityModifier`
- Crit Damage: `offensiveCritDamageModifier`
- All Damage: `offensiveTotalDamageModifier`
- Attack Speed: `characterAttackSpeedModifier`
- Cast Speed: `characterSpellCastSpeedModifier`
- Cooldown reduction: `skillCooldownReduction`, `skillCooldownReductionChance`
- Skill ranks: `augmentSkillName*`, `augmentSkillLevel*`
- Skill modifiers: `modifiedSkillName*`, `skillModifiers`
- Conversion: `conversionInType`, `conversionOutType`, `conversionPercentage`
- Resistance reduction: `offensive*ResistanceReduction*`
- Defense: Health, DA, resistances, absorption, sustain

### Resistance reduction

Treat relevant Resistance Reduction as a damage multiplier against enemies, not as ordinary percentage damage. Classify it by exact game wording before ranking:

| Wording | Behavior | Ranking rule |
|---|---|---|
| `-X% <type> Resistance` | Distinct sources stack additively and can reduce resistance below zero. | Usually the highest-value RR category. Add every relevant, reliably maintained source. |
| `X Reduced Target's <type> Resistance` | Flat reduction. Sources with the same affected resistance do not stack; only the highest active value applies. | Count only the improvement over the build's existing highest source, plus any coverage improvement. |
| `X% Reduced Target's <type> Resistance` | Multiplicative reduction. Sources with the same affected resistance do not stack; only the highest active value applies. | Count only the improvement over the existing highest source. Value it more against high positive resistance and less near zero. |

The three categories work together. Apply flat `X Reduced`, then multiplicative `X% Reduced`, then stacking `-X%` when estimating final resistance. For a resistance expressed in percentage points:

1. Subtract the highest active flat reduction.
2. Multiply a non-negative result by `1 - X / 100`, or a negative result by `1 + X / 100`, using the highest active multiplicative reduction.
3. Subtract the sum of relevant stacking `-X%` sources.
4. When `resistanceBefore < 100`, compare damage with `(100 - resistanceAfter) / (100 - resistanceBefore)`. Analyze the damage-enabling breakpoint separately when resistance starts at or above 100.

Treat `X% Chance of ...` as a trigger around one of these categories, not as another RR category. Estimate uptime from trigger eligibility, chance, action rate, duration, cooldown, and OA when the trigger requires a critical hit. Do not equate nominal RR with permanent RR when uptime is incomplete. Resistance Reduction delivered through Weapon Damage scales below 100% Weapon Damage and reaches full effect at 100%.

Weight only resistance types matching the build's actual post-conversion damage. For mixed damage, weight each type by its sustained damage share. An all-resistance source and a type-specific non-stacking source overlap for that type; use the stronger active value rather than adding them.

In raw data, `offensiveTotalResistanceReductionAbsolute*` represents flat all-resistance reduction and `offensiveTotalResistanceReductionPercent*` represents multiplicative all-resistance reduction. `Chance`, `DurationMin`, and `Min` companion fields describe activation, duration, and magnitude. Physical and Elemental variants use the corresponding `offensivePhysical*` and `offensiveElemental*` fields. Inspect direct `stats`, `skillModifiers`, proc data, and `unmodeledFields`; never infer stacking behavior from a field name without confirming its wording and source.

## BiS priorities

Use these as dependency-aware guidelines, not a fixed score formula.

For sustained direct weapon damage:

1. Main-skill, filler, and WPS ranks or modifiers weighted by cycle usage
2. Relevant, non-redundant Resistance Reduction with reliable uptime
3. Matching flat damage scaled by Weapon Damage across all eligible actions
4. Attack Speed or Cast Speed that adds filler, WPS, or trigger opportunities below cap
5. OA and OA-supported Crit Damage across the rotation
6. Matching percentage damage, All Damage, and useful conversion
7. Defense and sustain that preserve combat uptime

For damage over time:

1. Relevant skill ranks and meaningful flat DoT
2. Relevant, non-redundant Resistance Reduction active before application
3. OA and Crit Damage
4. Matching DoT percentage and duration
5. Speed only when it improves application or direct-hit output
6. Survivability

Lead with the sustained rotation winner. Compare single-hit burst separately when it favors a different affix, and never relabel that burst winner as the overall BiS without an explicit burst objective. Close results should remain conditional when build context can reverse them. Prefer a tied tier over invented precision. A generally strong affix is not automatically BiS for a specific build.

## Output

Lead with the BiS recommendation only when evidence supports it. Include:

1. Exact `itemClass`, Ascended category when used, filters, sustained objective, inferred rotation, and assumptions
2. Separate Prefix, Suffix, and Ascended candidate evaluations
3. Record IDs, parsed names, relevant ranges, and skill modifier references
4. Complete Prefix plus Suffix combinations and their marginal value over a repeated cycle
5. Sustained recommendation first, followed by a single-hit recommendation only when it differs
6. A short reason and reversal condition for each rank
7. At least one conditional alternative when context is incomplete

Do not output a bare score. Claim compatibility only from `affixes --type` or `affixes --family ascended --category` results. Do not label a result BiS when missing build information can materially reverse it. Do not answer a sustained-build question from the main skill's single-hit stats alone.
