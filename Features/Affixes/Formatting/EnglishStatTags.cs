namespace GdCli.Features.Affixes.Formatting;

internal sealed class EnglishStatTags : IStatTagProvider
{
    public EnglishStatTags(IReadOnlyDictionary<string, string> gameTags)
    {
        foreach (var tag in gameTags)
            _addIfMissing(tag.Key, tag.Value);

        var damageTypes = DamageTypeCatalog.BodyFields;
        var resistance = GetTag("Resistance");
        var toMaxResistance = GetTag("ResistanceMaxResist");

        foreach (var damageType in damageTypes)
        {
            var displayName = _resolveDamageType(damageType);
            _addIfMissing($"defensive{damageType}", $"{{0}}% {displayName} {resistance}");
            _addIfMissing($"defensive{damageType}Resistance", $"{{0}}% {displayName} {resistance}");
            _addIfMissing($"defensive{damageType}MaxResist", $"{{0}}% {toMaxResistance}{displayName} {resistance}");
        }
    }

    /// <summary>
    /// Resolve a display name for a damage type.
    /// </summary>
    /// <param name="damageType">The raw damage type.</param>
    /// <returns>The English display name.</returns>
    private string _resolveDamageType(string damageType)
    {
        damageType = damageType.Replace("Modifier", "", StringComparison.Ordinal);

        var localized = GetTag(damageType);

        if (!string.IsNullOrEmpty(localized))
        {
            return localized;
        }

        return damageType.Replace("Base", "", StringComparison.Ordinal);
    }

    private void _addIfMissing(string tag, string value)
    {
        _tags.TryAdd(tag, value);
    }

    private readonly Dictionary<string, string> _tags = new() {

            // Simply Header stats
            {"offensivePierceRatioMin", "{0}% Armor Piercing"},
            {"defensiveProtection", "{0} Armor"},
            {"defensiveStun", "{0}% Reduced Stun Duration"},
            {"petBurstSpawn", "+{0} Summons to {3}"},
            {"skillChanceWeight", "{0}% Chance to be Used"},
            {"skillProjectileNumber", "{0} Projectile"},
            {"skillCooldownTime", "{0} Seconds Skill Recharge"},
            {"skillManaCost", "{0} Energy Cost"},
            {"skillTargetRadius", "{0} Meter Radius"},
            {"skillActiveDuration", "{0} Second Duration"}, // Skills only

            // Simply Body Stats
            {"weaponDamagePct", "+{0}% Weapon Damage"},

            {"offensivePercentCurrentLifeMin", "{0}% Reduction to Enemy\"s Health"},
            {"characterLife", "+{0} Health"},
            {"characterLifeModifier", "+{0}% Health"},
            {"augmentAllLevel", "+{0} to All Skills"},
            {"characterDefensiveAbility", "+{0} Defensive Ability"},
            {"characterOffensiveAbility", "+{0} Offensive Ability"},
            {"characterDefensiveAbilityModifier", "+{0}% Defensive Ability"},
            {"characterOffensiveAbilityModifier", "+{0}% Offensive Ability"},
            {"augmentMastery1", "+{0} to All Skills in {3}"},
            {"augmentMastery2", "+{0} to All Skills in {3}"},
            {"augmentMastery3", "+{0} to All Skills in {3}"},
            {"augmentMastery4", "+{0} to All Skills in {3}"},
            {"augmentSkill1", "+{0} to {3}"},
            {"augmentSkill2", "+{0} to {3}"},
            {"augmentSkill3", "+{0} to {3}"},
            {"augmentSkill4", "+{0} to {3}"},
            {"augmentSkill1Extras", "Tier {0} {3} skill"},
            {"augmentSkill2Extras", "Tier {0} {3} skill"},
            {"augmentSkill3Extras", "Tier {0} {3} skill"},
            {"augmentSkill4Extras", "Tier {0} {3} skill"},
            {"defensivePetrify", "{0}% Reduced Petrify Duration"},
            {"offensiveCritDamageModifier", "+{0}% Crit Damage"},
            {"characterRunSpeedModifier", "+{0}% Movement Speed"},
            {"characterIncreasedExperience", "+{0}% Experience Gain"},
            {"characterIntelligenceModifier", "+{0}% Spirit"},
            {"skillCooldownReduction", "-{0}% Skill Cooldown Reduction"},
            {"retaliationTotalDamageModifier", "+{0}% Total Retaliation Damage"}, // not caught by the .replace("Modifier") for some reason..
            {"characterAttackSpeedModifier", "+{0}% Attack Speed"},
            {"characterAttackSpeed", "+{0}% Attack Speed"},
            {"offensiveLifeLeechMin", "{0}% of Attack Damage converted to Health"},
            {"characterIntelligence", "+{0} Spirit"},
            {"characterManaRegen", "+{0} Energy Regenerated per second"},
            {"characterManaRegenModifier", "+{0}% Energy Regenerated per second"},
            {"characterLightRadius", "+{0}% Light Radius"},
            {"characterDodgePercent", "+{0}% Chance to Avoid Melee Attacks"},
            {"piercingProjectile", "{0}% Chance to pass through Enemies"},
            {"characterMana", "+{0} Energy"},
            {"characterManaModifier", "+{0}% Energy"},
            {"characterEnergyAbsorptionPercent", "{0}% Energy Absorption From Enemy Spells"},
            {"characterSpellCastSpeedModifier", "+{0}% Casting Speed"},
            {"defensiveReflect", "{0}% Damage Reflected"},
            {"blockRecoveryTime", "{0} second Block Recovery"},
            {"characterLifeRegen", "+{0} Health regenerated per second"},
            {"characterDexterity", "+{0} Cunning"},
            {"characterDexterityModifier", "+{0}% Cunning"},
            {"defensiveTrap", "{0}% Reduced Entrapment Duration"},
            {"characterLifeRegenModifier", "+{0}% Health regenerated per second"},
            {"characterDeflectProjectile", "+{0}% Chance to Avoid Projectiles"},
            {"characterConstitutionModifier", "+{0}% Constitution"},
            {"characterHuntingDexterityReqReduction", "-{0}% Cunning Req. for Ranged Weapons"},
            {"characterStrength", "+{0} Physique"},
            {"characterStrengthModifier", "+{0}% Physique"},
            {"characterTotalSpeedModifier", "+{0}% Total Speed"}, // Deprecated?
            {"skillProjectileSpeedModifier", "+{0}% Increase in Projectile Speed"}, // Deprecated?
            {"defensiveAbsorptionModifier", " Increases Armor Absorption by {0}%"},
            {"defensiveAllMaxResist", "+{0} Maximum All Resistances"},
            {"skillLifeBonus", "{0} Health Restored"},
            {"defensiveFreeze", "{0}% Reduced Freeze Duration"},
            {"skillLifePercent", "{0}% Health Restored"},
            {"defensiveTotalSpeedResistance", "{0}% Slow Resistance"},
            {"defensiveBlockModifier", "Increases Shield Block Chance by {0}%"},
            {"offensiveTauntMin", "Taunt target for {0} Seconds"},
            {"defensiveBlockAmountModifier", "+{0}% Shield Damage Blocked"},
            {"characterDefensiveBlockRecoveryReduction", "+{0}% Shield Recovery Time"},
            {"skillTargetAngle", "{0} Targets in a {1}° Angle"},
            {"offensiveStunMin", "Stun target for {0} Seconds"},
            {"offensiveStunMax", "Stun target for {0}-{1} Seconds"},
            {"offensiveStunChance", "{3}% chance to "},
            {"defensiveProtectionModifier", "Increases Armor by {0}%"},
            {"projectilePiercingChance", "{0}% Chance to pass through Enemies"},
            {"characterGlobalReqReduction", "{0}% Reduction to Attribute Requirements"},
            {"projectileExplosionRadius", "{0} Meter Radius"},
            {"sparkChance", "{0}% Chance of affecting up to {1} targets within {2} Meters"},

            // Skill triggers
            {"cast_@allyonattack", " "},
            {"cast_@allyonlowhealth", " "},
            {"cast_@enemylocationonkill", " "},
            {"cast_@enemyonanyhit", " "},
            {"cast_@enemyonattack", "{0}% Chance on attack"},
            {"cast_@enemyonattackcrit", "{0}% Chance on a critical attack (target enemy)"},
            {"cast_@enemyonblock", "{0}% Chance when blocked"},
            {"cast_@enemyonhitcritical", "{0}% Chance when hit by a critical"},
            {"cast_@enemyonkill", "{0}% Chance on Enemy Death"}, //C
            {"cast_@enemyonmeleehit", "{0}% Chance when hit by a melee attack"},
            {"cast_@enemyonprojectilehit", "{0}% NPSkill Proc"},
            {"cast_@selfonanyhit", "{0}% Chance when hit"}, //C
            {"cast_@selfonattack", "{0}% Chance on attacking"},
            {"cast_@selfonattackcrit", "{0}% Chance on a critical attack"},
            {"cast_@selfonblock", "{0}% Chance when blocking"},
            {"cast_@selfonhitcritica", "{0}% Chance when Hit by a Critical"}, //C
            {"cast_@selfonkill", "{0}% Chance on Enemy Death"}, //C
            {"cast_@selfonlowhealth", "{0}% Chance at 25% health"},
            {"cast_@selfonmeleehit", "{0}% Chance when Hit by Melee Attacks"},
            {"cast_@selfonprojectilehit", "{0}% Chance when Hit by Ranged Attacks"},
            {"cast_@selfat", "{0}% Chance at {1}% Health"},

            // Retaliation
            {"customtag_013_retaliation", "{0}-{1} {3} Retaliation"},
            {"customtag_03_retaliation", "{0} {3} Retaliation"},
            {"customtag_retaliation_delay", " over {4} Seconds"},
            {"customtag_slow_retaliation", "{0}% reduced attack speed for {1}s"},

            // Damage
            {"customtag_damage_chanceof", "{0}% Chance of "},
            {"customtag_damage_123", "{1}-{2} {3} Damage"},
            {"customtag_damage_13", "{1} {3} Damage"},
            {"customtag_damage_13%", "+{1}% {3} Damage"},
            {"customtag_damage_delay", " over {4} Seconds"},
            {"customtag_damage_racial", "+{0}% Damage to {3}"},
            {"customtag_damage_racial02", "+{0}% Damage to {3} & {5}"},
            {"customtag_damage_conversion", "{0}% {3} Damage converted to {5}"},

            // Xpac
            {"customtag_xpac_modif_weaponDamagePct", "{0}% Weapon Damage to {3}"},
            {"customtag_xpac_modif_petLimit", "+{0} to Pet Limit to {3}"},
            {"customtag_xpac_modif_physicalResist", "{0} Reduced target's Physical Resistance to {3}"},
            {"customtag_xpac_modif_physicalResistDuration", "{0} Reduced target's Physical Resistance for {1} Seconds to {3}"},
            {"customtag_xpac_modif_dmgConversionPerc", "100% {5} Damage converted to {6} Damage to {3}"},
            {"customtag_xpac_modif_dmgConversion", "{5} Damage converted to {6} Damage to {3}"},
            {"customtag_xpac_modif_speedModifier", "-{0}% Total Speed to {3}"},
            {"customtag_xpac_modif_characterAttackSpeedModifier", "+{0}% Attack Speed to {3}"},
            {"customtag_xpac_modif_defensiveAbilityDebuff", "{0} Defensive Ability to {3}"},
            {"customtag_xpac_modif_defensiveAbilityBuff", "+{0}% Defensive Ability to {3}"},
            {"customtag_xpac_modif_offensiveAbilityBuff", "+{0}% Offensive Ability to {3}"},
            {"customtag_xpac_modif_offensiveTaunt", "Generate Additional Threat to {3}"},
            {"customtag_xpac_modif_addProjectileX", "{0} Projectiles to {3}"},
            {"customtag_xpac_modif_addProjectile1", "1 Projectile to {3}"},
            {"customtag_xpac_modif_offensiveDamageMinMax", "{0}-{1} {5} Damage to {3}"},
            {"customtag_xpac_modif_offensiveDamageMin", "{0} {5} Damage to {3}"},
            {"customtag_xpac_modif_skillManaCostReduction", "-{0}% Skill Energy Cost to {3}"},
            {"customtag_xpac_modif_skillTargetRadius", "{0} Meter Target Area to {3}"},
            {"customtag_xpac_modif_sparkChance", "{0}% Chance of affecting up to {1} targets to {3}"},
            {"customtag_xpac_modif_skillCooldownTime", "{0} Second Skill Recharge to {3}"},
            {"customtag_xpac_modif_offensiveCritDamageModifier", "+{0}% Crit Damage to {3}"},
            {"customtag_xpac_modif_skillActiveDuration", "{0} Second Duration to {3}"},
            {"customtag_xpac_modif_defense", "{0}% {5} Resistance to {3}"},
            {"customtag_xpac_modif_defensiveAbilityDebuffForDuration", "{1} Reduced target's Defensive Ability for {0} Seconds to {3}"},
            {"customtag_xpac_modif_skillLifePercent", "{0}% Health Restored to {3}"},
            {"customtag_xpac_modif_skillTargetAngle", "{0} Degree Attack Arc to {3}"},
            {"customtag_xpac_modif_skillTargetNumber", "{0} Target Maximum to {3}"},
            {"customtag_xpac_modif_skillCooldownReductionChance", "{0}% Chance of +{1}% Skill Cooldown Reduction to {3}"},
            {"customtag_xpac_modif_offensiveTotalDamageReductionPercentDurationMin", "{0}% Reduced target's Damage for {1} Seconds to {3}"},
            {"customtag_xpac_modif_offensiveTotalResistanceReductionAbsoluteMin", "{0} Reduced target's Resistances for {1} Seconds to {3}"},
            {"customtag_xpac_modif_offensiveDamageMultModifier", "Total Damage Modified by {0}% to {3}"},
            {"customtag_xpac_modif_retaliationTotalDamageModifier", "+{0}% to All Retaliation Damage to {3}"},
            {"offensiveXDurationModifier", "+{1}% {5} Damage with +{0}% Increased Duration to {3}"},

            {"racialBonusPercentDefense", "+{0}% Less Damage From {3}"},
            {"racialBonusPercentDefense02", "+{0}% Less Damage From {3} & {5}"},

            {"customtag_faction_boost", "+{0}% faction gain with {3}"},
            {"User7", "Black Legion"},
            {"User2", "Homestead"},
            {"User4", "Outcast"},
            {"User8", "Kymon's Chosen"},
            {"Survivors", "Devil's Crossing"},
            {"User0", "The Rovers"},
            {"User5", "Order of Death's Vigil"},
            {"Aetherials", "Aetherials"},
            {"Cthonians", "Cthonians"},
            {"Outlaws", "Outlaws"},
            {"User6", "Undead"},

            {"customtag_block_012", "{0}% Chance to Block {1} Damage ({2}% Absorption)"},
            {"customtag_block_01", "{0}% Chance to Block {1} Damage"},
            {"customtag_speed", "Speed: {3} ({0})"},

            {"customtag_resistance_reduction", "{0}% Chance of {1}% Reduced target's Resistance For {2} Seconds"},

            // Races
            {"Race001", "Undead"},
            {"Race002", "Beastkin"},
            {"Race003", "Aetherials"},
            {"Race004", "Chthonic"},
            {"Race005", "Aether Corruption"},
            {"Race009", "Human"},
            {"Race012", "Beastkin"},

            // Attack speeds
            {"tagAttackSpeedVeryFast", "Very Fast"},
            {"tagAttackSpeedFast", "Fast"},
            {"tagAttackSpeedAverage", "Average"},
            {"tagAttackSpeedSlow", "Slow"},
            {"tagAttackSpeedVerySlow", "Very Slow"},

            // Damage types
            {"SlowPhysical", "Internal Trauma"},
            {"SlowFire", "Burn"},
            {"SlowCold", "Frost"},
            {"SlowLightning", "Electrocute"},
            {"SlowVitality", "Vitality Decay"},
            {"SlowPoison", "Poison"},
            {"SlowLife", "Vitality Decay"},
            {"SlowBleeding", "Bleeding"},
            {"Poison", "Acid"},
            {"BasePoison", "Acid"},
            {"BonusPhysical", "Bonus"},
            {"Life", "Vitality"},
            {"TotalDamage", "to All"},
            {"PercentCurrentLife", "Life Reduction"},

            {"Physical", "Physical"},
            {"Fire", "Fire"},
            {"Cold", "Cold"},
            {"Vitality", "Vitality"},
            {"Lightning", "Lightning"},
            {"Chaos", "Chaos"},
            {"Bleeding", "Bleeding"},
            {"Elemental", "Elemental"},
            {"Pierce", "Pierce"},
            {"Aether", "Aether"},

            {"BasePhysical", "Physical"},
            {"BaseFire", "Fire"},
            {"BaseCold", "Cold"},
            {"BaseVitality", "Vitality"},
            {"BaseLightning", "Lightning"},
            {"BaseChaos", "Chaos"},
            {"BaseBleeding", "Bleeding"},
            {"BaseElemental", "Elemental"},
            {"BasePierce", "Pierce"},
            {"BaseAether", "Aether"},
            {"BaseLife", "Vitality"},
            {"Resistance", "Resistance"},
            {"ResistanceMaxResist", "to Maximum "},


            {"damageType_Physical", "Physical"},
            {"damageType_Fire", "Fire"},
            {"damageType_Cold", "Cold"},
            {"damageType_Vitality", "Vitality"},
            {"damageType_Lightning", "Lightning"},
            {"damageType_Chaos", "Chaos"},
            {"damageType_Bleeding", "Bleeding"},
            {"damageType_Elemental", "Elemental"},
            {"damageType_Pierce", "Pierce"},
            {"damageType_Aether", "Aether"},

            // Damage version (ref spanish "el fuego", "al fuego")
            {"damageType_BasePhysical", "Physical"},
            {"damageType_BaseFire", "Fire"},
            {"damageType_BaseCold", "Cold"},
            {"damageType_BaseVitality", "Vitality"},
            {"damageType_BaseLightning", "Lightning"},
            {"damageType_BaseChaos", "Chaos"},
            {"damageType_BaseBleeding", "Bleeding"},
            {"damageType_BaseElemental", "Elemental"},
            {"damageType_BasePierce", "Pierce"},
            {"damageType_BaseAether", "Aether"},
            {"damageType_BaseLife", "Vitality"},
            {"damageType_SlowPhysical", "Internal Trauma"},
            {"damageType_SlowFire", "Burn"},
            {"damageType_SlowCold", "Frost"},
            {"damageType_SlowLightning", "Electrocute"},
            {"damageType_SlowVitality", "Vitality Decay"},
            {"damageType_SlowPoison", "Poison"},
            {"damageType_SlowLife", "Vitality Decay"},
            {"damageType_SlowBleeding", "Bleeding"},
            {"damageType_Poison", "Acid"},
            {"damageType_BasePoison", "Acid"},
            {"damageType_BonusPhysical", "Bonus"},
            {"damageType_Life", "Vitality"},
            {"damageType_TotalDamage", "to All"},
            {"damageType_PercentCurrentLife", "Life Reduction"},

        };

    public string GetTag(string tag)
    {
        return _tags.TryGetValue(tag, out var value) ? value : string.Empty;
    }
}
