# Affix Ranking

Rank current affix records from `gd-cli`, not names or remembered tier lists.

## Contents

- [Scope](#scope)
- [Query workflow](#query-workflow)
- [Damage families](#damage-families)
- [Supporting stats](#supporting-stats)
- [Stat interpretation](#stat-interpretation)
- [Ranking priorities](#ranking-priorities)
- [Combination rules](#combination-rules)
- [Output](#output)

## Scope

- Require a damage type. Infer it only when unambiguous.
- Treat equipment type, build, mastery, main skill, attack or cast style, OA, target DA, speed cap, cooldowns, conversion, and defenses as optional context.
- Default Top N to 5.
- Use raw `stats` for analysis and English `effects` for presentation.
- Treat `schema.capabilities.itemAffixCompatibility = false` as authoritative. The CLI cannot prove that an affix is legal for an equipment type. For equipment-specific ranking, rank only candidate records supplied by the user or confirmed by another authoritative source. Never infer compatibility from an affix name.
- Do not include Ascended affixes. They are outside the current CLI schema.

## Query workflow

1. Run `schema` only when raw rarity or kind values are unknown.
2. Filter by `--kind`, `--rarity`, and level before projection.
3. Query exact known candidates with `affix <record-id>`.
4. For discovery, query prefixes and suffixes separately. Use `--all` only with a narrow projection.
5. Check `unmodeledFields`. State uncertainty when a relevant field is not modeled.

```powershell
.\gd-cli.exe affixes --kind prefix --rarity Rare --all --query "data[].{recordId:recordId,name:name,rarity:rarity,itemLevel:itemLevel,stats:stats,effects:effects,unmodeledFields:unmodeledFields}"
.\gd-cli.exe affixes --kind suffix --rarity Rare --all --query "data[].{recordId:recordId,name:name,rarity:rarity,itemLevel:itemLevel,stats:stats,effects:effects,unmodeledFields:unmodeledFields}"
.\gd-cli.exe affix <record-id>
```

CLI-owned labels are English. Parsed names and game tag values use the game-data language selected during `init`. Do not load another translation tree or invent translations.

## Damage families

| Damage type | Raw stat family |
|---|---|
| Physical | `offensiveBonusPhysical*`, `offensivePhysicalModifier` |
| Pierce | `offensivePierce*` |
| Fire | `offensiveFire*` |
| Cold | `offensiveCold*` |
| Lightning | `offensiveLightning*` |
| Acid | `offensivePoison*`, excluding `offensiveSlowPoison*` |
| Vitality | `offensiveLife*`, excluding `offensiveSlowLife*` |
| Aether | `offensiveAether*` |
| Chaos | `offensiveChaos*` |
| Elemental | `offensiveElemental*` |
| Internal Trauma | `offensiveSlowPhysical*` |
| Bleeding | `offensiveSlowBleeding*` |
| Burn | `offensiveSlowFire*` |
| Frostburn | `offensiveSlowCold*` |
| Electrocute | `offensiveSlowLightning*` |
| Poison | `offensiveSlowPoison*` |
| Vitality Decay | `offensiveSlowLife*` |

Direct and DoT pairs are Physical and Internal Trauma, Fire and Burn, Cold and Frostburn, Lightning and Electrocute, Acid and Poison, and Vitality and Vitality Decay. Bleeding is independent from Pierce. Aether and Chaos have no native DoT partner.

## Supporting stats

- `characterAttackSpeedModifier`: Attack Speed
- `characterSpellCastSpeedModifier`: Cast Speed
- `characterOffensiveAbility`: flat OA
- `characterOffensiveAbilityModifier`: percentage OA
- `offensiveCritDamageModifier`: Crit Damage
- `offensiveTotalDamageModifier`: All Damage
- `skillCooldownReduction`: deterministic Skill Cooldown Reduction
- `skillCooldownReductionChance`: chance-based Skill Cooldown Reduction
- `characterDefensiveAbility`: flat DA
- `characterLife`, `characterLifeModifier`: Health
- `augmentSkillName*`, `augmentSkillLevel*`: skill ranks
- `modifiedSkillName*`: build-specific skill modifiers
- `conversionInType`, `conversionOutType`, `conversionPercentage`: conversion
- `offensiveTotalResistanceReduction*`: resistance reduction requiring type, duration, trigger, and stacking context

Legacy fields map `characterStrength` to Physique, `characterDexterity` to Cunning, and `characterIntelligence` to Spirit.

## Stat interpretation

- Compare numeric `minimum` and `maximum` ranges. Do not rank by affix name alone.
- Convert chance-based flat damage to expected value for comparison: `chance / 100 * average(minimum, maximum)`. Still report the actual chance and range.
- Treat a minimum with no maximum as fixed unless an associated chance field exists.
- Distinguish flat damage, percentage damage, All Damage, OA, Crit Damage, speed, deterministic CDR, chance-based CDR, skill ranks, conversion, resistance reduction, and defense.
- Do not score an opaque granted skill or proc as known damage unless its effect is present in the returned data.
- Value skill bonuses and modifiers highly only when they match the build.
- Couple Crit Damage to OA. Give it no offensive value when the build cannot crit the priority target.
- Give deterministic CDR high value only when relevant skills are cooldown-limited.
- Never convert chance-based CDR into an equivalent global CDR percentage.

## Ranking priorities

### Direct weapon damage

Use this order as a reasoning guide, not a fixed formula:

1. Matching flat damage scaled by weapon damage, plus OA at realistic enemy DA
2. Relevant skill ranks or modifiers for a known build
3. Attack Speed until its cap, or Cast Speed for a casting build
4. Crit Damage when OA supports reliable critical hits
5. Matching percentage damage and All Damage
6. Useful conversion and resistance reduction with known context
7. Health, DA, resistances, and sustain as tie-breakers

Prefer an OA plus Crit Damage package over isolated Crit Damage. One speed affix plus one matching damage or All Damage affix is a strong generic baseline. Devalue additional speed near the cap.

### Damage over time

1. Relevant skill ranks and flat DoT from meaningful weapon-damage or skill sources
2. OA and Crit Damage
3. Resistance reduction active before application
4. Matching DoT percentage and duration
5. Attack or Cast Speed only when it improves application, chance-based effects, or direct-hit damage
6. Survivability, especially Reflected Damage Reduction for Internal Trauma

Repeated hits from the same DoT source generally refresh that source rather than stack it. Different sources can stack.

### Cooldown reduction

- Give deterministic CDR near-zero damage value for default attacks, WPS, no-cooldown spam skills, and builds with little cooldown-dependent output.
- Give it high value when main damage, rotation, devotion, defense, control, or buff uptime is cooldown-limited.
- Estimate the ideal cast-frequency ceiling as `old cooldown / new cooldown`, then reduce it for animation time, energy, positioning, overlap, and rotation limits.
- Do not apply global CDR to item-granted skills.
- Evaluate chance-based CDR separately and label its inconsistency.

## Combination rules

- Compare complete Prefix and Suffix packages. Do not merely pair the two largest percentage rolls.
- Consider matching flat damage, OA, OA-supported Crit Damage, speed, percentage damage, relevant skills, resistance reduction, conversion, and defense together.
- Use defense as a tie-breaker or a separate balanced recommendation. Do not hide a material offensive loss behind a small defensive gain.
- Do not claim exact DPS without the base item and full build.
- Prefer a tied tier or conditional ranking when missing build context can reverse close results.

## Output

Lead with the best general combination. Include:

1. Normalized damage type, Top N, supplied candidates, and assumptions
2. Prefix and Suffix record IDs and parsed names
3. Combined relevant ranges and expected values for chance-based damage
4. A short reason for each rank
5. The condition most likely to change each result
6. At least one conditional alternative when build context is missing

Do not output a bare score. Do not claim equipment compatibility unless it was supplied or independently confirmed.
