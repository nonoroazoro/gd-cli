namespace GdCli.Features.Affixes.Engine;

/// <summary>
/// Computes the real, seed-applied stat values for a Grim Dawn item by replaying the game's
/// shared MINSTD random stream over the item's rollable stats in the exact draw order used by
/// the game engine.
///
/// Draw order (per store, then field order within a store):
/// Char → flat added-damage min/max pairs → damage %-modifiers → retaliation flat min/max pairs →
/// retaliation Dur (Slow/DoT) block → retaliation %-modifiers → retaliation Reflex (CC) block →
/// Defense (block modifier/amount-modifier first, then resists) → conversions → Skill (deferred,
/// rolls last). Offensive damage modifiers and flat added damage take the item's
/// <c>attributeScalePercent</c> (float32) after jitter; retaliation and block never scale;
/// conversions use a float multiplicative jitter. Shield block fields are loaded raw with no
/// jitter at all (0 draws). Retaliation Dur's DurationMin and Reflex's Chance companions are
/// fixed (0 draws, echoed at base value).
///
/// </summary>
internal static class ItemStatEngine
{
    private const double _baseJitterPercent = 20.0;

    // ---- Store field orders, in the sequence the game's per-store loaders draw them ----

    private static readonly string[] _characterFields =
    {
        "characterStrength", "characterDexterity", "characterIntelligence", "characterLife", "characterMana",
        "characterStrengthModifier", "characterDexterityModifier", "characterIntelligenceModifier", "characterLifeModifier",
        "characterManaModifier", "characterLifeMultModifier", "characterOffensiveAbility", "characterDefensiveAbility",
        "characterOffensiveAbilityModifier", "characterDefensiveAbilityModifier", "characterLifeRegen", "characterLifeRegenModifier",
        "characterManaRegenModifier", "characterConstitutionModifier", "characterHealIncreasePercent", "characterTotalSpeedModifier",
        "characterAttackSpeedModifier", "characterAttackSpeedMaxModifier", "characterSpellCastSpeedModifier", "characterSpellCastSpeedMaxModifier",
        "characterRunSpeedModifier", "characterRunSpeedMaxModifier", "characterDefensiveBlockRecoveryReduction", "characterEnergyAbsorptionPercent",
        "characterDodgePercent", "characterDeflectProjectile", "characterManaLimitReserve", "characterManaLimitReserveModifier",
    };

    // Flat added-damage fields; each present {field}Min / {field}Max is one jittered draw (min
    // then spread), then scaled. Drawn after Char, before the %-modifiers.
    private static readonly string[] _flatDamageFields =
    {
        // offensivePhysicalMin/Max is fixed (0 draws, weapon base damage) only when the item's own
        // Class is a Weapon* class. For armor/jewelry the same field name is a real flat pair
        // (jittered + scaled, "N-M Physical Damage"); gated dynamically on Class in Compute().
        "offensivePhysical",
        "offensiveBonusPhysical", "offensivePierce", "offensiveFire", "offensiveCold", "offensiveLightning",
        "offensivePoison", "offensiveLife", "offensiveAether", "offensiveChaos", "offensiveElemental",
    };

    // offensiveSlow{Type}Min (+ Max, DurationMin): the per-tick damage-over-time flat value paired
    // with offensiveSlow{Type}Modifier. Draws as its own flat-tier entry right after the regular
    // Flat block and before the whole Dmg-modifier list. It uses the same (min, spread) mechanism as Flat,
    // scaled by attributeScalePercent. DurationMin is fixed (0 draws); the display total is
    // scaled_min * DurationMin (or a scaled_min..scaled_max range when a Max/spread is present).
    private static readonly string[] _damageOverTimeFields =
    {
        "offensiveSlowPhysical", "offensiveSlowBleeding", "offensiveSlowFire", "offensiveSlowCold",
        "offensiveSlowLightning", "offensiveSlowPoison", "offensiveSlowLife", "offensiveSlowAether", "offensiveSlowChaos",
        // Leech-over-time DoTs: same scaled min[,spread] * DurationMin mechanism as the damage-type
        // DoTs above (e.g. "72 Energy Leech over 3 Seconds").
        "offensiveSlowLifeLeach", "offensiveSlowManaLeach",
    };

    // Offensive crowd-control-on-hit. {field}Min is a duration in SECONDS (one jittered draw, NO
    // scale); {field}Chance is fixed (0 draws). Mechanically identical to _retaliationControlFields.
    private static readonly string[] _offensiveControlFields =
    {
        "offensiveStun", "offensiveKnockdown", "offensiveSleep", "offensiveFreeze", "offensivePetrify",
    };

    // Offensive slow/debuff duration effects: one jittered draw for the value; {field}DurationMin
    // fixed, {field}Chance fixed if present. Speed slows (TotalSpeed/AttackSpeed/SpellCastSpeed/
    // RunSpeed) scale by attributeScalePercent; ability reductions (Offensive/DefensiveAbility) do
    // not. The scale flag is per-field (see BuildOrder).
    private static readonly (string Field, bool Scaled)[] _offensiveSlowFields =
    {
        ("offensiveSlowTotalSpeed", true), ("offensiveSlowAttackSpeed", true),
        ("offensiveSlowSpellCastSpeed", true), ("offensiveSlowRunSpeed", true),
        ("offensiveSlowOffensiveAbility", false), ("offensiveSlowDefensiveAbility", false),
    };

    // Damage %-modifiers. Each offensiveSlow{Type}Modifier is drawn as a consecutive (value,
    // duration) pair with its offensiveSlow{Type}DurationModifier sibling (both scaled). Physical/
    // Bleeding/Fire/Cold/Lightning/Life/Poison have a DurationModifier field; Aether/Chaos only
    // have a DurationMin (which is fixed).
    private static readonly string[] _damageModifierFields =
    {
        "offensiveTotalDamageModifier", "offensiveCritDamageModifier",
        "offensivePhysicalModifier", "offensivePierceModifier", "offensiveFireModifier", "offensiveColdModifier", "offensiveLightningModifier",
        "offensivePoisonModifier", "offensiveLifeModifier", "offensiveAetherModifier", "offensiveChaosModifier", "offensiveElementalModifier",
        "offensiveSlowPhysicalModifier", "offensiveSlowPhysicalDurationModifier",
        "offensiveSlowBleedingModifier", "offensiveSlowBleedingDurationModifier",
        "offensiveSlowFireModifier", "offensiveSlowFireDurationModifier",
        "offensiveSlowColdModifier", "offensiveSlowColdDurationModifier",
        "offensiveSlowLightningModifier", "offensiveSlowLightningDurationModifier",
        "offensiveSlowPoisonModifier", "offensiveSlowPoisonDurationModifier",
        "offensiveSlowLifeModifier", "offensiveSlowLifeDurationModifier",
        "offensiveSlowAetherModifier", "offensiveSlowChaosModifier",
    };

    // offensive{Type}Modifier fields that use the same chance/non-chance proc-line split as
    // Flat/SlowFlat (see StatProcLine): a source carrying its own {Field}Chance becomes a separate
    // proc line instead of being summed into the merged total. Restricted to the type-specific
    // modifiers; Total/CritDamageModifier are excluded.
    private static readonly HashSet<string> _chanceSplitModifierFields = new(StringComparer.Ordinal)
    {
        "offensivePhysicalModifier", "offensivePierceModifier", "offensiveFireModifier", "offensiveColdModifier",
        "offensiveLightningModifier", "offensivePoisonModifier", "offensiveLifeModifier", "offensiveAetherModifier",
        "offensiveChaosModifier", "offensiveElementalModifier",
    };

    // offensiveLifeLeech: one jittered draw, Min-only, NOT scaled. Draws after all Dmg %-modifiers
    // and before the retaliation store. Displays "{v}% of Attack Damage converted to Health".
    private static readonly string[] _lifeLeechFields = { "offensiveLifeLeech" };

    // Offensive resistance/damage-reduction debuffs: value is one jittered draw (NO scale),
    // DurationMin fixed (0 draws). Draw after offensiveLifeLeech / the speed block, before the
    // retaliation store.
    private static readonly string[] _offensiveReductionFields =
    {
        "offensivePhysicalReductionPercent", "offensiveElementalReductionPercent",
        "offensiveTotalDamageReductionPercent", "offensiveTotalDamageReductionAbsolute",
        "offensiveTotalResistanceReductionPercent", "offensiveTotalResistanceReductionAbsolute",
        "offensivePhysicalResistanceReductionPercent", "offensivePhysicalResistanceReductionAbsolute",
        "offensiveElementalResistanceReductionPercent", "offensiveElementalResistanceReductionAbsolute",
    };

    // Retaliation flat damage: same (min, spread) pair mechanism as Flat, but retaliation never
    // scales. Drawn after the damage %-modifiers, before Defense.
    private static readonly string[] _retaliationFlatFields =
    {
        "retaliationPhysical", "retaliationPierce", "retaliationFire", "retaliationCold", "retaliationLightning",
        "retaliationPoison", "retaliationLife", "retaliationAether", "retaliationChaos", "retaliationElemental",
    };

    // Retaliation slow/DoT block: one jittered draw (no scale). Drawn after _retaliationFlatFields, before
    // _retaliationModifierFields. {field}DurationMin is fixed (0 draws, echoed base).
    private static readonly string[] _retaliationDurationFields =
    {
        "retaliationSlowPhysical", "retaliationSlowPierce", "retaliationSlowFire", "retaliationSlowCold",
        "retaliationSlowLightning", "retaliationSlowPoison", "retaliationSlowLife", "retaliationSlowAether",
        "retaliationSlowChaos", "retaliationSlowBleeding",
    };

    private static readonly string[] _retaliationSlowFields = { "retaliationSlowAttackSpeed", "retaliationSlowRunSpeed" };

    // Retaliation %-modifiers, drawn right after the _retaliationDurationFields block. retaliationDamageMultModifier
    // is the one exception: it draws AFTER the Defense store (see _postDefenseRetaliationModifierFields).
    private static readonly string[] _retaliationModifierFields =
    {
        "retaliationTotalDamageModifier",
        "retaliationPhysicalModifier", "retaliationPierceModifier", "retaliationFireModifier", "retaliationColdModifier",
        "retaliationLightningModifier", "retaliationPoisonModifier", "retaliationLifeModifier", "retaliationAetherModifier",
        "retaliationChaosModifier", "retaliationElementalModifier",
    };

    // retaliationDamageMultModifier draws after the Defense store, not with the rest of _retaliationModifierFields.
    private static readonly string[] _postDefenseRetaliationModifierFields = { "retaliationDamageMultModifier" };

    // Retaliation crowd-control block: one jittered draw (no scale). Drawn after _retaliationModifierFields, before
    // Defense. {field}Chance is fixed (0 draws, echoed base).
    private static readonly string[] _retaliationControlFields =
    {
        "retaliationStun", "retaliationFreeze", "retaliationConfusion",
    };

    // Defense store field order. defensiveBlockModifier/AmountModifier/ProtectionModifier load
    // first, then absorption, resists and the rest. Every entry is a single-value jittered draw
    // (default 20% jitter), NOT scaled by attributeScalePercent. The resistance caps (71-90,
    // defensive*MaxResist) draw no RNG and are fixed, so they are omitted from this list.
    private static readonly string[] _defenseFields =
    {
        "defensiveBlockModifier", "defensiveBlockAmountModifier", "defensiveProtectionModifier", // 01-03
        "defensiveAbsorptionModifier",                                                           // 04
        "defensivePhysical", "defensivePierce", "defensiveFire", "defensiveCold", "defensiveLightning", // 05-09
        "defensivePoison", "defensiveLife", "defensiveAether", "defensiveChaos",                 // 10-13
        "defensiveElementalResistance", "defensiveBleeding",                                     // 14-15
        "defensiveSlowLifeLeach", "defensiveSlowManaLeach",                                      // 16-17 (leech resist)
        "defensiveManaBurn", "defensiveAllResistance",                                           // 18-19
        "defensivePhysicalModifier", "defensivePierceModifier", "defensiveFireModifier", "defensiveColdModifier", // 20-23
        "defensiveLightningModifier", "defensivePoisonModifier", "defensiveLifeModifier", "defensiveAetherModifier", // 24-27
        "defensiveChaosModifier", "defensiveElementalModifier", "defensiveBleedingModifier",     // 28-30
        "defensiveSlowLifeLeachModifier", "defensiveSlowManaLeachModifier",                      // 31-32
        // DoT-resistance durations (33-43): "{v}% Reduction in {DoT} Duration"
        "defensivePhysicalDuration", "defensiveFireDuration", "defensiveColdDuration", "defensiveLightningDuration", // 33-36
        "defensivePoisonDuration", "defensiveLifeDuration", "defensiveAetherDuration", "defensiveChaosDuration", // 37-40
        "defensiveBleedingDuration", "defensiveSlowLifeLeachDuration", "defensiveSlowManaLeachDuration", // 41-43
        // DoT-resistance duration modifiers (44-54): "+{v}% {DoT} Duration Reduction"
        "defensivePhysicalDurationModifier", "defensiveFireDurationModifier", "defensiveColdDurationModifier", // 44-46
        "defensiveLightningDurationModifier", "defensivePoisonDurationModifier", "defensiveLifeDurationModifier", // 47-49
        "defensiveAetherDurationModifier", "defensiveChaosDurationModifier", "defensiveBleedingDurationModifier", // 50-52
        "defensiveSlowLifeLeachDurationModifier", "defensiveSlowManaLeachDurationModifier",      // 53-54
        "defensiveDisruption", "defensiveStun", "defensiveStunModifier", "defensiveFreeze",      // 55-58
        "defensiveTrap", "defensivePetrify", "defensiveSleep", "defensiveSleepModifier",         // 59-62
        "defensiveKnockdown", "defensiveKnockdownModifier", "defensiveTaunt", "defensiveFear",   // 63-66
        "defensiveConfusion", "defensiveConvert", "defensiveTotalSpeedResistance", "defensiveCrowdControl", // 67-70
        // (71-90 defense caps = fixed)
        "defensiveReflect", "defensiveReflectModifier", "defensivePercentCurrentLife", "defensivePercentReflectionResistance", // 91-94
    };

    // Shield block fields are read straight from the raw table with no jitter at all -> fixed,
    // 0 draws, echoed as-is.
    private static readonly string[] _fixedBlockFields =
    {
        "defensiveBlock", "defensiveBlockChance", "blockAbsorption", "blockRecoveryTime",
    };

    private static readonly string[] _conversionFields = { "conversionPercentage", "conversionPercentage2" };

    // Skill store, drawn LAST (deferred). Each field is one jittered draw. Only a few of these
    // appear on real items; the rest are listed for draw-order completeness. The per-field
    // {field}Chance companion is fixed (0 draws), see _isFixed().
    private static readonly string[] _skillFields =
    {
        "skillCooldownReduction", "skillManaCostReduction", "skillComboChargeSpendReduction",
        "skillProjectileSpeedModifier", "skillCooldownReductionModifier", "skillManaCostReductionModifier",
    };

    // These skill fields draw EARLY when affix-sourced (see DrawEarlySkillFields). Every other skill
    // field draws at the deferred Skill position, like the base record's own skill fields.
    private static readonly string[] _earlySkillFields = { "skillCooldownReduction", "skillManaCostReduction" };

    // crit/total damage modifiers do NOT take item scale.
    private static readonly HashSet<string> _nonScalingFields = new(StringComparer.Ordinal)
    {
        "offensiveCritDamageModifier", "offensiveTotalDamageModifier",
    };

    private static readonly List<StatOrderEntry> _fieldOrder = _buildFieldOrder();

    /// <summary>Every field the engine models (single fields + flat Min/Max pairs).</summary>
    private static readonly HashSet<string> _modeledFields = _buildModeledFields();

    private static List<StatOrderEntry> _buildFieldOrder()
    {
        var order = new List<StatOrderEntry>();
        _addFields(order, _characterFields, StatKind.Character);
        _addFields(order, _flatDamageFields, StatKind.Flat, true);
        _addFields(order, _damageOverTimeFields, StatKind.SlowFlat, true);
        foreach (var field in _damageModifierFields)
            order.Add(new StatOrderEntry(StatKind.Damage, field, !_nonScalingFields.Contains(field)));
        _addFields(order, _lifeLeechFields, StatKind.Leech);
        _addFields(order, _offensiveControlFields, StatKind.OffensiveReflex);
        foreach (var (field, scales) in _offensiveSlowFields)
            order.Add(new StatOrderEntry(StatKind.OffensiveSlow, field, scales));
        _addFields(order, _offensiveReductionFields, StatKind.OffensiveReduction);
        _addFields(order, _retaliationFlatFields, StatKind.RetaliationFlat);
        _addFields(order, _retaliationDurationFields, StatKind.RetaliationDuration);
        _addFields(order, _retaliationModifierFields, StatKind.RetaliationModifier);
        _addFields(order, _retaliationSlowFields, StatKind.RetaliationDuration);
        _addFields(order, _retaliationControlFields, StatKind.RetaliationReflex);
        _addFields(order, _defenseFields, StatKind.Defense);
        _addFields(order, _postDefenseRetaliationModifierFields, StatKind.RetaliationModifier);
        _addFields(order, _conversionFields, StatKind.Conversion);
        _addFields(order, _skillFields, StatKind.Skill);
        return order;
    }

    private static void _addFields(
        List<StatOrderEntry> order,
        IEnumerable<string> fields,
        StatKind kind,
        bool scales = false)
    {
        foreach (var field in fields)
            order.Add(new StatOrderEntry(kind, field, scales));
    }

    private static HashSet<string> _buildModeledFields()
    {
        var fields = new HashSet<string>(StringComparer.Ordinal);
        foreach (var entry in _fieldOrder)
        {
            switch (entry.Kind)
            {
                case StatKind.Flat:
                case StatKind.RetaliationFlat:
                case StatKind.SlowFlat:
                    fields.Add(entry.Field + "Min");
                    fields.Add(entry.Field + "Max");
                    break;
                case StatKind.RetaliationDuration:
                case StatKind.OffensiveSlow:
                    fields.Add(entry.Field + "Min");
                    fields.Add(entry.Field + "DurationMin");
                    fields.Add(entry.Field + "Chance");
                    break;
                case StatKind.RetaliationReflex:
                case StatKind.OffensiveReflex:
                    fields.Add(entry.Field + "Min");
                    fields.Add(entry.Field + "Chance");
                    break;
                case StatKind.Leech:
                    fields.Add(entry.Field + "Min");
                    break;
                case StatKind.OffensiveReduction:
                    fields.Add(entry.Field + "Min");
                    fields.Add(entry.Field + "DurationMin");
                    break;
                default:
                    fields.Add(entry.Field);
                    break;
            }
        }
        return fields;
    }

    // Fields that are present on items but never draw (echoed at base value).
    private static readonly HashSet<string> _fixedFields = new(StringComparer.Ordinal)
    {
        "characterBaseAttackSpeed", "characterManaRegen", "characterConstitution", "characterAttackSpeed",
        "characterSpellCastSpeed", "characterRunSpeed", "characterIncreasedExperience", "characterIncreasedGold",
        "characterLightRadius", "characterGlobalReqReduction", "characterLevelReqReduction", "characterModifierPoints",
        // (characterHealIncreasePercent jitters and lives in the Char list. It is not fixed.)
        "defensiveProtection",          // armor: 0 draws
    };

    private static bool _isFixed(string field)
    {
        if (_fixedFields.Contains(field)) return true;
        if (field.StartsWith("character", StringComparison.Ordinal) && field.EndsWith("ReqReduction", StringComparison.Ordinal)) return true;
        // weapon base damage: 0 draws, shown at base range.
        if (field.StartsWith("offensiveBase", StringComparison.Ordinal) && (field.EndsWith("Min", StringComparison.Ordinal) || field.EndsWith("Max", StringComparison.Ordinal))) return true;
        if (_fixedBlockFields.Contains(field)) return true;
        // offensiveSlow*DurationMin: fixed (0 draws), same as the retaliation Dur block's DurationMin.
        if (field.StartsWith("offensiveSlow", StringComparison.Ordinal) && field.EndsWith("DurationMin", StringComparison.Ordinal)) return true;
        // offensive{X}RatioMin (e.g. offensivePierceRatioMin -> "100% Armor Piercing"): fixed, 0 draws.
        if (field.StartsWith("offensive", StringComparison.Ordinal) && field.EndsWith("RatioMin", StringComparison.Ordinal)) return true;
        // Per-field proc {value}Chance companions (offensive*/retaliation*/skill*) are loaded but
        // never jittered (0 draws); they prefix the modeled value line as "{chance}% Chance of ...".
        if (field.EndsWith("Chance", StringComparison.Ordinal) && !field.EndsWith("GlobalChance", StringComparison.Ordinal)
            && (field.StartsWith("offensive", StringComparison.Ordinal) || field.StartsWith("retaliation", StringComparison.Ordinal)
                || field.StartsWith("skill", StringComparison.Ordinal)))
            return true;
        // Grouped-proc config: offensive/retaliationGlobalChance ("N% Chance of:" group header) and
        // each participating effect's *Global flag group several proc effects under one shared roll
        // instead of each effect's own Chance; they consume no RNG.
        if (field is "offensiveGlobalChance" or "retaliationGlobalChance") return true;
        if ((field.StartsWith("offensive", StringComparison.Ordinal) || field.StartsWith("retaliation", StringComparison.Ordinal))
            && field.EndsWith("Global", StringComparison.Ordinal))
            return true;
        return false;
    }

    private static readonly string[] _cosmeticPrefixes =
    {
        "augment", "modif", "item", "drop", "physics", "mesh", "bitmap", "baseTexture", "bumpTexture", "glowTexture", "shader",
        "weaponTrail", "hitSound", "swipeSound", "blockSound", "attackEffect", "basicProjectile", "actor", "casts", "maxTransparency",
        "outline", "scale", "templateName", "Class", "FileDescription", "levelRequirement", "itemLevel", "armorClassification",
        "characterBaseAttackSpeedTag", "armorFemaleMesh", "armorMaleMesh", "decoration", "attributeScalePercent", "petBonusName",
    };

    private static readonly string[] _statPrefixes =
    {
        "offensive", "defensive", "retaliation", "character", "skill", "conversion", "blockAbsorption", "blockRecovery",
    };

    /// <summary>
    /// A rollable stat field the engine does not model. Its presence risks desyncing the shared
    /// stream, so downstream computed values may be wrong.
    /// </summary>
    private static bool _isConcerning(string field)
    {
        if (_cosmeticPrefixes.Any(prefix => field.StartsWith(prefix, StringComparison.Ordinal))) return false;
        if (_modeledFields.Contains(field) || _isFixed(field)) return false;
        if (field.StartsWith("conversion", StringComparison.Ordinal)) return false;
        return _statPrefixes.Any(prefix => field.StartsWith(prefix, StringComparison.Ordinal));
    }

    /// <summary>
    /// Calculates theoretical lower and upper stat boundaries without treating fixed fields as rolls.
    /// </summary>
    internal static StatRangeResult ComputeRange(
        IEnumerable<StatInput> stats,
        double? scalePercentOverride = null,
        IEnumerable<StatInput>? prefixStats = null,
        IEnumerable<StatInput>? suffixStats = null)
    {
        return new StatRangeResult(
            _computeCore(stats, new BoundRandom(false), scalePercentOverride, prefixStats, suffixStats),
            _computeCore(stats, new BoundRandom(true), scalePercentOverride, prefixStats, suffixStats));
    }

    private static StatComputationResult _computeCore(
        IEnumerable<StatInput> stats,
        IRollSource rollSource,
        double? scalePercentOverride,
        IEnumerable<StatInput>? prefixStats,
        IEnumerable<StatInput>? suffixStats)
    {
        var (values, text) = _parseStats(stats);
        bool hasPrefix = prefixStats is not null;
        bool hasSuffix = suffixStats is not null;
        var (prefixValues, prefixText) = _parseStats(prefixStats ?? []);
        var (suffixValues, suffixText) = _parseStats(suffixStats ?? []);

        // Absence of lootRandomizerJitter means that affix's own contribution never jitters (fixed,
        // 0 draws). Defaulting to 0 here (rather than the base record's 20%) reproduces that:
        // StatJitter.ApplyIntegerRoll/Skill treat a 0 percent as a no-draw.
        double prefixJitterPercent = prefixValues.GetValueOrDefault("lootRandomizerJitter", 0.0);
        double suffixJitterPercent = suffixValues.GetValueOrDefault("lootRandomizerJitter", 0.0);
        double affixScalePercent = prefixValues.GetValueOrDefault("lootRandomizerScale", 0.0)
            + suffixValues.GetValueOrDefault("lootRandomizerScale", 0.0);

        double scalePercent = scalePercentOverride
            ?? (values.GetValueOrDefault("attributeScalePercent", 0.0) + affixScalePercent);

        var result = new Dictionary<string, double>(StringComparer.Ordinal);
        var ambiguousConversionFields = new List<string>();
        var procLines = new List<StatProcLine>();
        var handledDurationFields = new HashSet<string>(StringComparer.Ordinal);

        bool ScalarPresent(string field) => values.ContainsKey(field) || prefixValues.ContainsKey(field) || suffixValues.ContainsKey(field);
        bool isOffHand = text.TryGetValue("Class", out var itemClass) && itemClass == "WeaponArmor_Offhand";

        // Skill-store fields (skillCooldownReduction/skillManaCostReduction) sourced from an AFFIX
        // Draw early at the start of the Damage store (before the item's first present non-Char
        // entry), prefix first then suffix, not at the deferred end-of-sequence Skill position
        // where the base record's own skill fields draw. The early value is the affix's
        // contribution, summed with the base's end-drawn part.
        bool prefixHasEarlySkill = hasPrefix && _earlySkillFields.Any(prefixValues.ContainsKey);
        bool suffixHasEarlySkill = hasSuffix && _earlySkillFields.Any(suffixValues.ContainsKey);
        bool earlySkillFieldsDrawn = !(prefixHasEarlySkill || suffixHasEarlySkill);
        var earlySkillValues = new Dictionary<string, double>(StringComparer.Ordinal);
        void DrawEarlySkillFields()
        {
            if (earlySkillFieldsDrawn) return;
            earlySkillFieldsDrawn = true;
            if (prefixHasEarlySkill)
                foreach (var field in _earlySkillFields)
                    if (prefixValues.TryGetValue(field, out var value))
                        earlySkillValues[field] = earlySkillValues.GetValueOrDefault(field) + StatJitter.ApplySkillRoll(value, prefixJitterPercent, rollSource);
            if (suffixHasEarlySkill)
                foreach (var field in _earlySkillFields)
                    if (suffixValues.TryGetValue(field, out var value))
                        earlySkillValues[field] = earlySkillValues.GetValueOrDefault(field) + StatJitter.ApplySkillRoll(value, suffixJitterPercent, rollSource);
        }

        foreach (var entry in _fieldOrder)
        {
            switch (entry.Kind)
            {
                case StatKind.Flat:
                    {
                        if (entry.Field == "offensivePhysical"
                            && text.TryGetValue("Class", out var baseItemClass)
                            && baseItemClass.StartsWith("Weapon", StringComparison.Ordinal))
                            break; // weapon base physical: 0 draws, shown as base range (fixed)
                        string minimumField = entry.Field + "Min", maximumField = entry.Field + "Max";
                        bool anyPresent = values.ContainsKey(minimumField) || values.ContainsKey(maximumField)
                            || prefixValues.ContainsKey(minimumField) || prefixValues.ContainsKey(maximumField)
                            || suffixValues.ContainsKey(minimumField) || suffixValues.ContainsKey(maximumField);
                        if (!anyPresent) break;
                        DrawEarlySkillFields();
                        // The value-array pair is (min, spread) where spread = max(0, max - min).
                        // AddJitter jitters BOTH elements; the scale pass scales ONLY the min. So max is
                        // derived as jittered_min + jittered_spread, never jittered independently. For
                        // affixed items each present source (base/prefix/suffix) is its own
                        // independently-jittered (min, spread) pair using that source's own jitterPercent; the raw
                        // jittered mins are summed across sources and scale is applied ONCE to the total.
                        // The spread element is never scaled. On a caster off-hand
                        // (Class=WeaponArmor_Offhand, cannot attack) an affix's flat pair is discarded
                        // entirely (0 draws, no line).
                        string chanceField = entry.Field + "Chance";
                        double finalMinimum = 0.0, finalSpread = 0.0;
                        bool anyDrawn = false;
                        void AccumulateFlat(Dictionary<string, double> sourceValues, double jitterPercent, bool active, bool isBase)
                        {
                            if (!active) return;
                            bool hasMin = sourceValues.ContainsKey(minimumField), hasMax = sourceValues.ContainsKey(maximumField);
                            if (!hasMin && !hasMax) return;
                            if (!isBase && isOffHand) return; // off-hand: affix flat destroyed, no draw
                            double minimum = sourceValues.GetValueOrDefault(minimumField, 0.0), maximum = sourceValues.GetValueOrDefault(maximumField, 0.0);
                            double rolledMinimum = StatJitter.ApplyIntegerRoll(minimum, jitterPercent, rollSource);
                            double rolledSpread = StatJitter.ApplyIntegerRoll(Math.Max(0.0, maximum - minimum), jitterPercent, rollSource);
                            // The chance-bearing-source-is-its-own-line split only applies in an
                            // affix-merge context (hasPrefix||hasSuffix). A base-only item's own
                            // per-field Chance is just its group's displayed chance (0 draws, no merge
                            // risk since there's only ever one source).
                            if ((hasPrefix || hasSuffix) && sourceValues.TryGetValue(chanceField, out var chance))
                            {
                                double scaledMinimum = StatJitter.ApplyScale(rolledMinimum, scalePercent);
                                double scaledMaximum = Math.Truncate(scaledMinimum + rolledSpread);
                                scaledMinimum = Math.Truncate(scaledMinimum);
                                procLines.Add(new StatProcLine(entry.Field, scaledMinimum, scaledMaximum == scaledMinimum ? null : scaledMaximum, null, chance));
                                return;
                            }
                            anyDrawn = true;
                            finalMinimum += rolledMinimum;
                            finalSpread += rolledSpread;
                        }
                        AccumulateFlat(values, _baseJitterPercent, true, true);
                        AccumulateFlat(prefixValues, prefixJitterPercent, hasPrefix, false);
                        AccumulateFlat(suffixValues, suffixJitterPercent, hasSuffix, false);
                        if (!anyDrawn) break;
                        finalMinimum = StatJitter.ApplyScale(finalMinimum, scalePercent);
                        result[minimumField] = Math.Truncate(finalMinimum);
                        result[maximumField] = Math.Truncate(finalMinimum + finalSpread);
                        break;
                    }
                case StatKind.SlowFlat:
                    {
                        // Per-source sum-then-scale, same mechanism as Flat above; the display total is
                        // scale(sum of jittered mins) * DurationMin. DurationMin stays fixed (echoed via
                        // the fixed-field pass below). Off-hand focus items (Class=WeaponArmor_Offhand)
                        // do not draw or display their own SlowFlat pair at all, even when the row
                        // carries it, gated on Class like the Flat case.
                        if (isOffHand) break;
                        string minimumField = entry.Field + "Min", maximumField = entry.Field + "Max", durationField = entry.Field + "DurationMin";
                        // A base Min with no DurationMin at all can't render "over N Seconds": 0 draws,
                        // not displayed.
                        if (values.ContainsKey(minimumField) && !values.ContainsKey(durationField)) break;
                        bool anyPresent = values.ContainsKey(minimumField) || values.ContainsKey(maximumField)
                            || prefixValues.ContainsKey(minimumField) || prefixValues.ContainsKey(maximumField)
                            || suffixValues.ContainsKey(minimumField) || suffixValues.ContainsKey(maximumField);
                        if (!anyPresent) break;
                        DrawEarlySkillFields();
                        string slowChanceField = entry.Field + "Chance";
                        double finalMinimum = 0.0, finalSpread = 0.0;
                        bool anyDrawn = false;
                        void AccumulateSlowFlat(Dictionary<string, double> sourceValues, double jitterPercent)
                        {
                            bool hasMin = sourceValues.ContainsKey(minimumField), hasMax = sourceValues.ContainsKey(maximumField);
                            if (!hasMin && !hasMax) return;
                            double minimum = sourceValues.GetValueOrDefault(minimumField, 0.0), maximum = sourceValues.GetValueOrDefault(maximumField, 0.0);
                            double rolledMinimum = StatJitter.ApplyIntegerRoll(minimum, jitterPercent, rollSource);
                            double rolledSpread = StatJitter.ApplyIntegerRoll(Math.Max(0.0, maximum - minimum), jitterPercent, rollSource);
                            // A source carrying its own {Field}Chance is a separate proc DoT line (own
                            // scale, own DurationMin), not summed into the merged non-chance bucket.
                            // same split as the Flat case above; only in an affix-merge context.
                            if ((hasPrefix || hasSuffix) && sourceValues.TryGetValue(slowChanceField, out var chance))
                            {
                                double scaledMinimum = StatJitter.ApplyScale(rolledMinimum, scalePercent);
                                double scaledMaximum = Math.Truncate(scaledMinimum + rolledSpread);
                                scaledMinimum = Math.Truncate(scaledMinimum);
                                double? sourceDuration = sourceValues.TryGetValue(durationField, out var duration) ? duration : null;
                                procLines.Add(new StatProcLine(entry.Field, scaledMinimum, scaledMaximum == scaledMinimum ? null : scaledMaximum, sourceDuration, chance));
                                return;
                            }
                            anyDrawn = true;
                            finalMinimum += rolledMinimum;
                            finalSpread += rolledSpread;
                        }
                        AccumulateSlowFlat(values, _baseJitterPercent);
                        if (hasPrefix) AccumulateSlowFlat(prefixValues, prefixJitterPercent);
                        if (hasSuffix) AccumulateSlowFlat(suffixValues, suffixJitterPercent);
                        if (!anyDrawn) break;
                        finalMinimum = StatJitter.ApplyScale(finalMinimum, scalePercent);
                        result[minimumField] = Math.Truncate(finalMinimum);
                        result[maximumField] = Math.Truncate(finalMinimum + finalSpread);
                        break;
                    }
                case StatKind.RetaliationFlat:
                    {
                        string minimumField = entry.Field + "Min", maximumField = entry.Field + "Max";
                        bool anyPresent = values.ContainsKey(minimumField) || values.ContainsKey(maximumField)
                            || prefixValues.ContainsKey(minimumField) || prefixValues.ContainsKey(maximumField)
                            || suffixValues.ContainsKey(minimumField) || suffixValues.ContainsKey(maximumField);
                        if (!anyPresent) break;
                        DrawEarlySkillFields();
                        // Same (min, spread) pair mechanism as Flat above, but retaliation never scales.
                        // Affix sources summed component-wise the same way as Flat.
                        double retaliationMinimum = 0.0, retaliationSpread = 0.0;
                        void AccumulateRetal(Dictionary<string, double> sourceValues, double jitterPercent, bool active, bool isBase)
                        {
                            if (!active) return;
                            bool hasMin = sourceValues.ContainsKey(minimumField), hasMax = sourceValues.ContainsKey(maximumField);
                            if (!hasMin && !hasMax) return;
                            double minimum = sourceValues.GetValueOrDefault(minimumField, 0.0), maximum = sourceValues.GetValueOrDefault(maximumField, 0.0);
                            retaliationMinimum += StatJitter.ApplyIntegerRoll(minimum, jitterPercent, rollSource);
                            retaliationSpread += StatJitter.ApplyIntegerRoll(Math.Max(0.0, maximum - minimum), jitterPercent, rollSource);
                        }
                        AccumulateRetal(values, _baseJitterPercent, true, true);
                        AccumulateRetal(prefixValues, prefixJitterPercent, hasPrefix, false);
                        AccumulateRetal(suffixValues, suffixJitterPercent, hasSuffix, false);
                        result[minimumField] = Math.Truncate(retaliationMinimum);
                        result[maximumField] = Math.Truncate(retaliationMinimum + retaliationSpread);
                        break;
                    }
                case StatKind.RetaliationDuration:
                case StatKind.RetaliationReflex:
                case StatKind.OffensiveReflex:
                    {
                        // Affix-sourced retaliation Dur/Reflex and offensive-CC pairs draw at this same
                        // position, each source with its own jitterPercent, contributions summed; DurationMin/
                        // Chance stay fixed (taken from whichever source has them, base first). _offensiveControlFields
                        // is mechanically identical to _retaliationControlFields: Min = seconds, one jittered draw (no
                        // scale), Chance fixed.
                        string minimumField = entry.Field + "Min";
                        bool present = values.ContainsKey(minimumField) || prefixValues.ContainsKey(minimumField) || suffixValues.ContainsKey(minimumField);
                        if (!present) break;
                        DrawEarlySkillFields();
                        double total = 0.0;
                        if (values.TryGetValue(minimumField, out var baseMinimum)) total += StatJitter.ApplyIntegerRoll(baseMinimum, _baseJitterPercent, rollSource);
                        if (hasPrefix && prefixValues.TryGetValue(minimumField, out var prefixMinimum)) total += StatJitter.ApplyIntegerRoll(prefixMinimum, prefixJitterPercent, rollSource);
                        if (hasSuffix && suffixValues.TryGetValue(minimumField, out var suffixMinimum)) total += StatJitter.ApplyIntegerRoll(suffixMinimum, suffixJitterPercent, rollSource);

                        result[minimumField] = entry.Kind == StatKind.RetaliationReflex ? total : Math.Round(total, MidpointRounding.AwayFromZero);
                        foreach (var component in entry.Kind == StatKind.RetaliationDuration ? new[] { "DurationMin", "Chance" } : new[] { "Chance" })
                        {
                            string componentField = entry.Field + component;
                            if (values.TryGetValue(componentField, out var baseComponentValue)) result[componentField] = baseComponentValue;           // fixed, 0 draws
                            else if (prefixValues.TryGetValue(componentField, out var prefixComponentValue)) result[componentField] = prefixComponentValue;
                            else if (suffixValues.TryGetValue(componentField, out var suffixComponentValue)) result[componentField] = suffixComponentValue;
                        }
                        break;
                    }
                case StatKind.Conversion:
                    {
                        // Each source's conversionPercentage is jittered separately (base with the
                        // default 20%, an affix with its own lootRandomizerJitter), drawing in
                        // base → prefix → suffix order; contributions with the same In/Out type pair
                        // sum for display.
                        string fieldSuffix = entry.Field.EndsWith('2') ? "2" : "";
                        string inputTypeField = "conversionInType" + fieldSuffix, outputTypeField = "conversionOutType" + fieldSuffix;
                        var conversionTotals = new Dictionary<(string, string), double>();
                        var conversionOrder = new List<(string, string)>();
                        void AccumulateConv(Dictionary<string, double> sourceValues, Dictionary<string, string> sourceText, double jitterPercent, bool active)
                        {
                            if (!active) return;
                            double value = sourceValues.GetValueOrDefault(entry.Field, 0.0);
                            sourceText.TryGetValue(inputTypeField, out var inputType);
                            sourceText.TryGetValue(outputTypeField, out var outputType);
                            if (string.IsNullOrEmpty(inputType) || value == 0.0) return; // invalid: destroyed, no draw
                            DrawEarlySkillFields();
                            var key = (inputType!, outputType ?? "");
                            if (!conversionTotals.ContainsKey(key)) conversionOrder.Add(key);
                            conversionTotals[key] = conversionTotals.GetValueOrDefault(key) + StatJitter.ApplyConversionRoll(value, jitterPercent, rollSource);
                        }
                        AccumulateConv(values, text, _baseJitterPercent, true);
                        AccumulateConv(prefixValues, prefixText, prefixJitterPercent, hasPrefix);
                        AccumulateConv(suffixValues, suffixText, suffixJitterPercent, hasSuffix);
                        if (conversionOrder.Count == 0) break;
                        result[entry.Field] = conversionTotals[conversionOrder[0]];
                        if (conversionOrder.Count > 1)
                            ambiguousConversionFields.Add(entry.Field + " (multiple distinct conversion type pairs)");
                        break;
                    }
                case StatKind.Leech:
                    {
                        // offensiveLifeLeech: per-source jitter (own jitterPercent), summed, no scale, Min-only.
                        // Damage store is base-first (base then prefix then suffix).
                        string minimumField = entry.Field + "Min";
                        if (!(values.ContainsKey(minimumField) || prefixValues.ContainsKey(minimumField) || suffixValues.ContainsKey(minimumField))) break;
                        DrawEarlySkillFields();
                        double total = 0.0;
                        if (values.TryGetValue(minimumField, out var baseLeechValue)) total += StatJitter.ApplyIntegerRoll(baseLeechValue, _baseJitterPercent, rollSource);
                        if (hasPrefix && prefixValues.TryGetValue(minimumField, out var prefixLeechValue)) total += StatJitter.ApplyIntegerRoll(prefixLeechValue, prefixJitterPercent, rollSource);
                        if (hasSuffix && suffixValues.TryGetValue(minimumField, out var suffixLeechValue)) total += StatJitter.ApplyIntegerRoll(suffixLeechValue, suffixJitterPercent, rollSource);
                        result[minimumField] = total;   // no scale
                        break;
                    }
                case StatKind.OffensiveSlow:
                    {
                        // Offensive speed-slow / ability-reduction debuff: per-source jitter (own jitterPercent),
                        // summed; speed slows scale (entry.Scales, applied once to the summed value), ability
                        // reductions do not. DurationMin/Chance fixed (0 draws, echoed from the first
                        // source that has them, base first).
                        string minimumField = entry.Field + "Min";
                        if (!(values.ContainsKey(minimumField) || prefixValues.ContainsKey(minimumField) || suffixValues.ContainsKey(minimumField))) break;
                        DrawEarlySkillFields();
                        double total = 0.0;
                        if (values.TryGetValue(minimumField, out var baseSlowValue)) total += StatJitter.ApplyIntegerRoll(baseSlowValue, _baseJitterPercent, rollSource);
                        if (hasPrefix && prefixValues.TryGetValue(minimumField, out var prefixSlowValue)) total += StatJitter.ApplyIntegerRoll(prefixSlowValue, prefixJitterPercent, rollSource);
                        if (hasSuffix && suffixValues.TryGetValue(minimumField, out var suffixSlowValue)) total += StatJitter.ApplyIntegerRoll(suffixSlowValue, suffixJitterPercent, rollSource);
                        result[minimumField] = entry.Scales ? StatJitter.ApplyScale(total, scalePercent) : Math.Round(total, MidpointRounding.AwayFromZero);
                        foreach (var component in new[] { "DurationMin", "Chance" })
                        {
                            string componentField = entry.Field + component;
                            if (values.TryGetValue(componentField, out var baseComponentValue)) result[componentField] = baseComponentValue;           // fixed, 0 draws
                            else if (prefixValues.TryGetValue(componentField, out var prefixComponentValue)) result[componentField] = prefixComponentValue;
                            else if (suffixValues.TryGetValue(componentField, out var suffixComponentValue)) result[componentField] = suffixComponentValue;
                        }
                        break;
                    }
                case StatKind.OffensiveReduction:
                    {
                        // Offensive reduction debuff: per-source jitter (own jitterPercent), summed, no scale;
                        // DurationMin fixed (0 draws, echoed from the first source that has it, base first).
                        string minimumField = entry.Field + "Min", durationField = entry.Field + "DurationMin";
                        if (!(values.ContainsKey(minimumField) || prefixValues.ContainsKey(minimumField) || suffixValues.ContainsKey(minimumField))) break;
                        DrawEarlySkillFields();
                        double total = 0.0;
                        if (values.TryGetValue(minimumField, out var baseReductionValue)) total += StatJitter.ApplyIntegerRoll(baseReductionValue, _baseJitterPercent, rollSource);
                        if (hasPrefix && prefixValues.TryGetValue(minimumField, out var prefixReductionValue)) total += StatJitter.ApplyIntegerRoll(prefixReductionValue, prefixJitterPercent, rollSource);
                        if (hasSuffix && suffixValues.TryGetValue(minimumField, out var suffixReductionValue)) total += StatJitter.ApplyIntegerRoll(suffixReductionValue, suffixJitterPercent, rollSource);
                        result[minimumField] = total;   // no scale
                        if (values.TryGetValue(durationField, out var baseDuration)) result[durationField] = baseDuration;         // fixed, 0 draws
                        else if (prefixValues.TryGetValue(durationField, out var prefixDuration)) result[durationField] = prefixDuration;
                        else if (suffixValues.TryGetValue(durationField, out var suffixDuration)) result[durationField] = suffixDuration;
                        break;
                    }
                default:
                    {
                        // Scalar kinds (Char/Dmg/Def/_retaliationModifierFields/Skill): each source has its own value and
                        // own jitter jitterPercent, then summed. With no prefix/suffix supplied this reduces to the
                        // base-only behavior exactly (prefixValue/suffixValue are 0, and Jitter.* is a no-draw for a 0
                        // value). Draw order differs by store: Char/Skill/_retaliationModifierFields draw
                        // Prefix → Suffix → Base (base last); Dmg and Def draw Base first, then Prefix,
                        // then Suffix.
                        if (!ScalarPresent(entry.Field)) break;
                        if (entry.Kind != StatKind.Character) DrawEarlySkillFields();
                        double prefixValue = prefixValues.GetValueOrDefault(entry.Field, 0.0);
                        double suffixValue = suffixValues.GetValueOrDefault(entry.Field, 0.0);
                        double baseValue = values.GetValueOrDefault(entry.Field, 0.0);
                        if (entry.Kind == StatKind.Damage && handledDurationFields.Contains(entry.Field))
                            break; // already drawn as part of its Slow{X}Modifier pair below
                        if (entry.Kind == StatKind.Damage
                            && entry.Field.StartsWith("offensiveSlow", StringComparison.Ordinal)
                            && entry.Field.EndsWith("Modifier", StringComparison.Ordinal)
                            && !entry.Field.EndsWith("DurationModifier", StringComparison.Ordinal))
                        {
                            string durationField = entry.Field[..^"Modifier".Length] + "DurationModifier";
                            if (ScalarPresent(durationField))
                            {
                                // Slow{X}Modifier + Slow{X}DurationModifier live in one object per source,
                                // so each source draws its (value, duration) pair consecutively:
                                // base(v,d), prefix(v,d), suffix(v,d), not all values then all durations.
                                double totalValue = 0.0, totalDuration = 0.0;
                                void AccumulatePair(Dictionary<string, double> sourceValues, double jitterPercent, bool active)
                                {
                                    if (!active) return;
                                    totalValue += StatJitter.ApplyIntegerRoll(sourceValues.GetValueOrDefault(entry.Field, 0.0), jitterPercent, rollSource);
                                    totalDuration += StatJitter.ApplyIntegerRoll(sourceValues.GetValueOrDefault(durationField, 0.0), jitterPercent, rollSource);
                                }
                                AccumulatePair(values, _baseJitterPercent, true);
                                AccumulatePair(prefixValues, prefixJitterPercent, hasPrefix);
                                AccumulatePair(suffixValues, suffixJitterPercent, hasSuffix);
                                result[entry.Field] = entry.Scales ? StatJitter.ApplyScale(totalValue, scalePercent) : totalValue;
                                result[durationField] = StatJitter.ApplyScale(totalDuration, scalePercent);
                                handledDurationFields.Add(durationField);
                                break;
                            }
                        }
                        double prefixRoll, suffixRoll, baseRoll;
                        if (entry.Kind == StatKind.Skill)
                        {
                            bool early = _earlySkillFields.Contains(entry.Field) && (prefixHasEarlySkill || suffixHasEarlySkill);
                            if (early)
                            {
                                // Affix contributions were drawn early (see DrawEarlySkillFields); only the
                                // base record's own part draws here at the deferred Skill position.
                                baseRoll = StatJitter.ApplySkillRoll(baseValue, _baseJitterPercent, rollSource);
                                result[entry.Field] = earlySkillValues.GetValueOrDefault(entry.Field) + baseRoll;
                                break;
                            }
                            prefixRoll = hasPrefix ? StatJitter.ApplySkillRoll(prefixValue, prefixJitterPercent, rollSource) : 0.0;
                            suffixRoll = hasSuffix ? StatJitter.ApplySkillRoll(suffixValue, suffixJitterPercent, rollSource) : 0.0;
                            baseRoll = StatJitter.ApplySkillRoll(baseValue, _baseJitterPercent, rollSource);
                        }
                        else if (entry.Kind is StatKind.Damage or StatKind.Defense or StatKind.RetaliationModifier)
                        {
                            baseRoll = StatJitter.ApplyIntegerRoll(baseValue, _baseJitterPercent, rollSource);
                            prefixRoll = hasPrefix ? StatJitter.ApplyIntegerRoll(prefixValue, prefixJitterPercent, rollSource) : 0.0;
                            suffixRoll = hasSuffix ? StatJitter.ApplyIntegerRoll(suffixValue, suffixJitterPercent, rollSource) : 0.0;
                        }
                        else
                        {
                            prefixRoll = hasPrefix ? StatJitter.ApplyIntegerRoll(prefixValue, prefixJitterPercent, rollSource) : 0.0;
                            suffixRoll = hasSuffix ? StatJitter.ApplyIntegerRoll(suffixValue, suffixJitterPercent, rollSource) : 0.0;
                            baseRoll = StatJitter.ApplyIntegerRoll(baseValue, _baseJitterPercent, rollSource);
                        }
                        if (_chanceSplitModifierFields.Contains(entry.Field) && (hasPrefix || hasSuffix))
                        {
                            // Chance split: draws already happened above (base/prefix/suffix, order
                            // unaffected). A source carrying its own {Field}Chance is a separate proc
                            // line, NOT summed into the merged non-chance total.
                            string chanceField = entry.Field + "Chance";
                            double chanceSplitTotal = 0.0;
                            bool hasUnconditionalValue = false;
                            foreach (var (sourceValues, rolledValue) in new[] { (values, baseRoll), (prefixValues, prefixRoll), (suffixValues, suffixRoll) })
                            {
                                if (!sourceValues.ContainsKey(entry.Field)) continue;
                                if (sourceValues.TryGetValue(chanceField, out var chance) && chance > 0)
                                    procLines.Add(new StatProcLine(entry.Field, entry.Scales ? StatJitter.ApplyScale(rolledValue, scalePercent) : rolledValue, null, null, chance));
                                else
                                {
                                    hasUnconditionalValue = true;
                                    chanceSplitTotal += rolledValue;
                                }
                            }
                            if (hasUnconditionalValue) result[entry.Field] = entry.Scales
                                ? StatJitter.ApplyScale(chanceSplitTotal, scalePercent)
                                : chanceSplitTotal;
                            break;
                        }
                        double total = prefixRoll + suffixRoll + baseRoll;
                        result[entry.Field] = entry.Scales ? StatJitter.ApplyScale(total, scalePercent) : total;
                        break;
                    }
            }
        }

        // Echo fixed (zero-draw) stat fields at their base/prefix/suffix value (base wins if more
        // than one source defines the same fixed field. Summing fixed fields across sources is not
        // established, so this is a conservative best-effort choice).
        var unmodeledFields = new List<string>();
        var allFields = new HashSet<string>(values.Keys, StringComparer.Ordinal);
        allFields.UnionWith(prefixValues.Keys);
        allFields.UnionWith(suffixValues.Keys);
        foreach (var field in allFields)
        {
            if (result.ContainsKey(field)) continue;
            if (_isFixed(field) && _statPrefixes.Any(p => field.StartsWith(p, StringComparison.Ordinal)))
            {
                if (values.TryGetValue(field, out var baseValue)) result[field] = baseValue;
                else if (prefixValues.TryGetValue(field, out var prefixValue)) result[field] = prefixValue;
                else if (suffixValues.TryGetValue(field, out var suffixValue)) result[field] = suffixValue;
            }
            else if (_isConcerning(field))
                unmodeledFields.Add(field);
        }
        foreach (var field in ambiguousConversionFields)
            unmodeledFields.Add(field + " [affix pair field, not modeled]");
        unmodeledFields.Sort(StringComparer.Ordinal);

        return new StatComputationResult(result, unmodeledFields, procLines);
    }

    private static (Dictionary<string, double> Values, Dictionary<string, string> Text) _parseStats(IEnumerable<StatInput> stats)
    {
        var values = new Dictionary<string, double>(StringComparer.Ordinal);
        var text = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var stat in stats)
        {
            values[stat.Field] = stat.Value;
            if (!string.IsNullOrEmpty(stat.TextValue)) text[stat.Field] = stat.TextValue;
        }
        return (values, text);
    }
}
