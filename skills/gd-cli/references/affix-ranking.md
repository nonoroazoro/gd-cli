# BiS Affix Evaluation

Identify the best type-compatible Prefix, Suffix, and Ascended affixes for a specific equipment type and build.

## Required context

- Exact `itemClass` for Prefix and Suffix compatibility.
- Main skill, damage type, and attack or cast style.
- Game-native Ascended category when Ascended ranking is requested.

Mastery, base item, existing conversion, OA, target DA, speed caps, cooldowns, defenses, and the rest of the build improve accuracy. Without enough build context, return conditional candidates and do not claim a definitive BiS result.

## Candidate discovery

Resolve raw values from `info` or `schema`. Never infer compatibility from affix names.

```powershell
gd-cli affixes --type <itemClass> --kind prefix --all --query "data[].{recordId:recordId,name:name,rarity:rarity,itemLevel:itemLevel,requiredLevel:requiredLevel,stats:stats,effects:effects,unmodeledFields:unmodeledFields}"
gd-cli affixes --type <itemClass> --kind suffix --all --query "data[].{recordId:recordId,name:name,rarity:rarity,itemLevel:itemLevel,requiredLevel:requiredLevel,stats:stats,effects:effects,unmodeledFields:unmodeledFields}"
gd-cli ascended-affixes --category <category> --all --query "data[].{recordId:recordId,name:name,groups:groups,stats:stats,effects:effects,skillModifiers:skillModifiers,unmodeledFields:unmodeledFields}"
```

Apply `--rarity`, `--min-level`, and `--max-level` to normal affix queries when the request supplies those constraints. Use `affix <record-id>` or `ascended-affix <record-id>` for exact details.

Prefix, Suffix, and Ascended are independent systems:

- Evaluate Prefix candidates against Prefix candidates.
- Evaluate Suffix candidates against Suffix candidates.
- Rank Ascended candidates within the requested category and preserve their `groups` value.
- Evaluate complete Prefix plus Suffix combinations before declaring normal-affix BiS.
- Do not substitute Ascended for Prefix or Suffix.

## BiS evaluation

Use raw numeric `stats` for reasoning and English `effects` for presentation.

1. Remove stats unrelated to the requested damage type, skill, or objective.
2. Merge candidate stats with the base item and known build state when available.
3. Compare complete affix packages, not isolated headline values.
4. Compare `minimum` and `maximum`; treat equal values as fixed.
5. For chance-based damage, use `chance / 100 * average(minimum, maximum)` as expected value while retaining the original chance and range.
6. Value skill ranks and skill modifiers only when they affect the build.
7. Value Crit Damage only when OA permits critical hits against the target.
8. Devalue Attack Speed, Cast Speed, resistances, and other capped stats near their effective caps.
9. Value deterministic cooldown reduction only for cooldown-limited skills. Never convert chance-based cooldown reduction into equivalent global cooldown reduction.
10. Evaluate conversion and resistance reduction only with their direction, damage type, duration, trigger, and stacking context.
11. Detect overlap with stats already supplied by the base item or build. Marginal value determines BiS, not isolated magnitude.
12. Treat opaque procs and granted skills as unknown unless returned fields expose their effect.
13. Inspect `unmodeledFields`; report uncertainty when a relevant field is not modeled.
14. Do not claim exact DPS without the base item and full build.

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
- Resistance reduction: `offensiveTotalResistanceReduction*`
- Defense: Health, DA, resistances, absorption, sustain

## BiS priorities

Use these as dependency-aware guidelines, not a fixed score formula.

For direct weapon damage:

1. Relevant skill ranks or modifiers
2. Matching flat damage scaled by weapon damage
3. OA and OA-supported Crit Damage
4. Attack Speed or Cast Speed below cap
5. Matching percentage damage and All Damage
6. Useful conversion and resistance reduction
7. Defense and sustain as tie-breakers

For damage over time:

1. Relevant skill ranks and meaningful flat DoT
2. OA and Crit Damage
3. Resistance reduction active before application
4. Matching DoT percentage and duration
5. Speed only when it improves application or direct-hit output
6. Survivability

Close results should remain conditional when build context can reverse them. Prefer a tied tier over invented precision. A generally strong affix is not automatically BiS for a specific build.

## Output

Lead with the BiS recommendation only when evidence supports it. Include:

1. Exact `itemClass`, Ascended category when used, filters, objective, and assumptions
2. Separate Prefix, Suffix, and Ascended candidate evaluations
3. Record IDs, parsed names, relevant ranges, and skill modifier references
4. Complete Prefix plus Suffix combinations and their marginal value to the build
5. A short reason and reversal condition for each rank
6. At least one conditional alternative when context is incomplete

Do not output a bare score. Claim compatibility only from `affixes --type` or `ascended-affixes --category` results. Do not label a result BiS when missing build information can materially reverse it.
