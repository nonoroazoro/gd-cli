using System.Text;
using System.Text.RegularExpressions;

namespace GdCli.Features.Affixes.Formatting;

internal sealed partial class StatFormatter
{
    private readonly IStatTagProvider _tags;

    public StatFormatter(IStatTagProvider tags)
    {
        _tags = tags;
    }

    private void _processConversionDamage(ISet<StatValue> stats, List<FormattedStat> result)
    {
        var conversionPercentage = stats.FirstOrDefault(stat => stat.Field == "conversionPercentage");
        var conversionOutType = stats.FirstOrDefault(stat => stat.Field == "conversionOutType");
        var conversionInType = stats.FirstOrDefault(stat => stat.Field == "conversionInType");

        if (conversionPercentage != null && conversionOutType != null && conversionInType != null)
        {
            result.Add(new FormattedStat
            {
                Text = _tags.GetTag("customtag_damage_conversion"),
                Param0 = conversionPercentage.Value,
                Param3 = _resolveDamageType(conversionInType.TextValue),
                Param5 = _resolveDamageType(conversionOutType.TextValue),
                Type = StatSection.Body
            });
        }

        // Pet fields use the same schema with a "pet" prefix.
        var petConversionPercentage = stats.FirstOrDefault(stat => stat.Field == "petconversionPercentage");
        var petConversionOutType = stats.FirstOrDefault(stat => stat.Field == "petconversionOutType");
        var petConversionInType = stats.FirstOrDefault(stat => stat.Field == "petconversionInType");

        if (petConversionPercentage != null && petConversionOutType != null && petConversionInType != null)
        {
            result.Add(new FormattedStat
            {
                Text = _tags.GetTag("customtag_damage_conversion"),
                Param0 = petConversionPercentage.Value,
                Param3 = _resolveDamageType(petConversionInType.TextValue),
                Param5 = _resolveDamageType(petConversionOutType.TextValue),
                Type = StatSection.Body
            });
        }
    }

    /// <summary>
    /// Try to get the class name. Replaces [ms]{0}[fs]{1} format by {0}/{1}.
    /// </summary>
    /// <param name="initialTag">Initial tag</param>
    /// <returns>Class name</returns>
    private string _resolveClassName(string? initialTag)
    {
        if (string.IsNullOrEmpty(initialTag))
            return string.Empty;

        var className = _tags.GetTag(initialTag);

        // Probably a custom class. Try to get via tagSkillClassName.
        if (string.IsNullOrEmpty(className))
        {
            className = _tags.GetTag(initialTag.Replace(
                "class",
                "tagSkillClassName",
                StringComparison.Ordinal));
        }

        return _classNamePattern().IsMatch(className)
            ? _classNamePattern().Replace(className, "$1/$2")
            : className;
    }

    /// <summary>
    /// Process skill stats
    /// These are non-standard skills, pre-processed by the parser.
    ///
    /// augmentSkill contains both the skill name and the increment amount
    /// augmentSkillExtras contains any additional info like which class it belongs to, and tier.
    /// This is done to avoid cross-record/item lookups at runtime
    /// </summary>
    /// <param name="stats"></param>
    /// <param name="result"></param>
    private void _processAddSkill(ISet<StatValue> stats, List<FormattedStat> result)
    {
        var skillCandidates = stats.Where(stat =>
                stat.Field.StartsWith("augmentSkill", StringComparison.Ordinal)
                && stat.Field.Length == "augmentSkill".Length + 1)
            .ToList();

        // "augmentSkill1", "augmentSkill2",
        foreach (var stat in skillCandidates)
        {
            var statName = stat.Field;
            // Extra fields carry mastery and tier metadata for the matching skill slot.
            var statExtras = stats.FirstOrDefault(stat => stat.Field == statName + "Extras");

            FormattedStat? extraStat = null;

            if (statExtras != null)
            {
                extraStat = new FormattedStat
                {
                    Text = _tags.GetTag(statName + "Extras"),
                    Param0 = statExtras.Value, // Tier
                    Param3 = _resolveClassName(statExtras.TextValue)
                };
            }

            result.Add(new FormattedStat
            {
                Text = _tags.GetTag(statName),
                Param0 = stat.Value,
                Param3 = stat.TextValue,
                Extra = extraStat
            });
        }
    }
    private void _processAddMastery(ISet<StatValue> stats, List<FormattedStat> result)
    {
        // "augmentSkill1", "augmentSkill2",
        for (var i = 1; i <= 4; i++)
        {
            var statName = "augmentMastery" + i;
            var stat = stats.FirstOrDefault(stat => stat.Field == statName);

            if (stat != null)
            {
                result.Add(new FormattedStat
                {
                    Text = _tags.GetTag(statName),
                    Param0 = stat.Value,
                    Param3 = _resolveClassName(stat.TextValue)
                });
            }
        }
    }

    private void _processStun(ISet<StatValue> stats, List<FormattedStat> result)
    {
        var min = stats.FirstOrDefault(stat => stat.Field == "offensiveStunMin");
        var max = stats.FirstOrDefault(stat => stat.Field == "offensiveStunMax");
        var chance = stats.FirstOrDefault(stat => stat.Field == "offensiveStunChance");

        if (min == null)
        {
            return;
        }

        var tag = _tags.GetTag(max != null ? "offensiveStunMax" : "offensiveStunMin");

        if (chance != null)
        {
            tag = _tags.GetTag("offensiveStunChance") + tag;
        }

        result.Add(new FormattedStat
        {
            Text = tag,
            Param0 = min.Value,
            Param1 = max?.Value,
            Param3 = chance?.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)
        });
    }

    private void _processAngleDamage(ISet<StatValue> stats, List<FormattedStat> result)
    {
        var angle = stats.FirstOrDefault(stat => stat.Field == "skillTargetAngle");
        var numTargets = stats.FirstOrDefault(stat => stat.Field == "skillTargetNumber");

        if (angle != null && numTargets != null)
        {
            result.Add(new FormattedStat
            {
                Text = _tags.GetTag("skillTargetAngle"),
                Param0 = numTargets.Value,
                Param1 = angle.Value
            });
        }
    }

    private void _processChainDamage(ISet<StatValue> stats, List<FormattedStat> result)
    {
        var sparkChance = stats.FirstOrDefault(stat => stat.Field == "sparkChance");
        var sparkGap = stats.FirstOrDefault(stat => stat.Field == "sparkGap");
        var sparkMaxNumber = stats.FirstOrDefault(stat => stat.Field == "sparkMaxNumber");

        if (sparkChance != null && sparkGap != null && sparkMaxNumber != null)
        {
            result.Add(new FormattedStat
            {
                Text = _tags.GetTag("sparkChance"),
                Param0 = sparkChance.Value,
                Param1 = sparkMaxNumber.Value,
                Param2 = sparkGap.Value
            });
        }
    }

    private void _processFactionWrits(ISet<StatValue> stats, List<FormattedStat> result)
    {
        var faction = stats.FirstOrDefault(stat => stat.Field == "boostedFaction");
        var multiplier = stats.FirstOrDefault(stat => stat.Field == "boostedMultiplier");

        if (faction != null && multiplier != null)
        {
            result.Add(new FormattedStat
            {
                Text = _tags.GetTag("customtag_faction_boost"),
                Param0 = (multiplier.Value - 1) * 100,
                Param3 = _resolveTagValue(faction.TextValue),
                Type = StatSection.Body
            });
        }
    }

    private void _processReducedResistances(ISet<StatValue> stats, List<FormattedStat> result)
    {
        var chance = stats.FirstOrDefault(stat => stat.Field == "offensiveTotalResistanceReductionPercentChance");
        var duration = stats.FirstOrDefault(stat => stat.Field == "offensiveTotalResistanceReductionPercentDurationMin");
        var min = stats.FirstOrDefault(stat => stat.Field == "offensiveTotalResistanceReductionPercentMin");

        if (chance != null && duration != null && min != null)
        {
            result.Add(new FormattedStat
            {
                Text = _tags.GetTag("customtag_resistance_reduction"),
                Param0 = chance.Value,
                Param1 = min.Value,
                Param2 = duration.Value,
                Type = StatSection.Body
            });
        }
    }

    private void _processRacialBonuses(ISet<StatValue> stats, List<FormattedStat> result)
    {
        var racialBonusPercentDamage = stats.FirstOrDefault(stat => stat.Field == "racialBonusPercentDamage");
        var racialBonusRace = stats.Where(stat => stat.Field == "racialBonusRace").ToList();

        if (racialBonusPercentDamage != null && racialBonusRace.Count >= 1)
        {
            var race01 = _resolveTagValue(racialBonusRace[0].TextValue);
            var race02 = racialBonusRace.Count >= 2 ? _resolveTagValue(racialBonusRace[1].TextValue) : null;

            result.Add(new FormattedStat
            {
                Text = _tags.GetTag("customtag_damage_racial"),
                Param0 = racialBonusPercentDamage.Value,
                Param3 = race01,
                Param5 = race02,
                Type = StatSection.Body
            });
        }

        var racialBonusPercentDefense = stats.FirstOrDefault(stat => stat.Field == "racialBonusPercentDefense");

        if (racialBonusPercentDefense == null || racialBonusRace.Count < 1)
        {
            return;
        }

        var raceDef01 = _resolveTagValue(racialBonusRace[0].TextValue);
        var raceDef02 = racialBonusRace.Count >= 2 ? _resolveTagValue(racialBonusRace[1].TextValue) : null;

        if (raceDef02 == null)
        {
            result.Add(new FormattedStat
            {
                Text = _tags.GetTag("racialBonusPercentDefense"),
                Param0 = racialBonusPercentDefense.Value,
                Param3 = raceDef01,
                Type = StatSection.Body
            });
        }
        else
        {
            result.Add(new FormattedStat
            {
                Text = _tags.GetTag("racialBonusPercentDefense02"),
                Param0 = racialBonusPercentDefense.Value,
                Param3 = raceDef01,
                Param5 = raceDef02,
                Type = StatSection.Body
            });
        }
    }

    private void _processAttackSpeed(ISet<StatValue> stats, List<FormattedStat> result)
    {
        var characterBaseAttackSpeed = stats.FirstOrDefault(stat => stat.Field == "characterBaseAttackSpeed");

        if (characterBaseAttackSpeed == null)
        {
            return;
        }

        var tag = stats.FirstOrDefault(stat => stat.Field == "characterBaseAttackSpeedTag")?.TextValue;
        tag = tag != null ? _tags.GetTag(tag) : "Unknown";

        result.Add(new FormattedStat
        {
            Text = _tags.GetTag("customtag_speed"),
            Param0 = characterBaseAttackSpeed.Value,
            Param3 = tag
        });
    }

    private void _processSlowRetaliation(ISet<StatValue> stats, List<FormattedStat> result)
    {
        var duration = stats.FirstOrDefault(stat => stat.Field == "retaliationSlowAttackSpeedDurationMin");
        var amount = stats.FirstOrDefault(stat => stat.Field == "retaliationSlowAttackSpeedMin");

        if (duration != null && amount != null)
        {
            result.Add(new FormattedStat
            {
                Text = _tags.GetTag("customtag_slow_retaliation"),
                Param0 = amount.Value,
                Param1 = duration.Value
            });
        }
    }

    private void _processShieldBlock(ISet<StatValue> stats, List<FormattedStat> result)
    {
        var defensiveBlockChance = stats.FirstOrDefault(stat => stat.Field == "defensiveBlockChance");

        if (defensiveBlockChance == null)
        {
            return;
        }

        var defensiveBlock = stats.FirstOrDefault(stat => stat.Field == "defensiveBlock");
        var blockAbsorption = stats.FirstOrDefault(stat => stat.Field == "blockAbsorption");

        if (defensiveBlock != null && blockAbsorption != null)
        {
            result.Add(new FormattedStat
            {
                Text = _tags.GetTag("customtag_block_012"),
                Param0 = defensiveBlockChance.Value,
                Param1 = defensiveBlock.Value,
                Param2 = blockAbsorption.Value,
                Type = StatSection.Header
            });
        }
        else if (defensiveBlock != null)
        {
            result.Add(new FormattedStat
            {
                Text = _tags.GetTag("customtag_block_01"),
                Param0 = defensiveBlockChance.Value,
                Param1 = defensiveBlock.Value,
                Type = StatSection.Header
            });
        }
    }

    private string _resolveDamageType(string? damageType)
    {
        if (string.IsNullOrEmpty(damageType))
            return string.Empty;

        damageType = damageType.Replace("Modifier", "", StringComparison.Ordinal);

        var localized = _tags.GetTag($"damageType_{damageType}");

        if (!string.IsNullOrEmpty(localized))
        {
            return localized;
        }

        localized = _tags.GetTag(damageType);
        if (!string.IsNullOrEmpty(localized))
        {
            return localized;
        }

        return damageType.Replace("Base", "", StringComparison.Ordinal);
    }

    private string _resolveTagValue(string? tag)
    {
        return string.IsNullOrEmpty(tag) ? string.Empty : _tags.GetTag(tag);
    }

    private void _processHeaderDamage(ISet<StatValue> stats, List<FormattedStat> result)
    {
        string[] headerDamageTypes = {
                "BasePoison",
                "BaseChaos",
                "BaseFire",
                "BaseAether",
                "BaseCold",
                "BaseLightning",
                "BasePierce",
                "BasePhysical",
                "BaseLife"
            };
        var damageTypes = headerDamageTypes.Select(damageType => $"offensive{damageType}Min").ToList();
        _processDamage(stats, result, damageTypes, StatSection.Header);
    }

    private void _processBodyDamage(ISet<StatValue> stats, List<FormattedStat> result)
    {
        var damageTypes = DamageTypeCatalog.BodyFields.Select(damageType => $"offensive{damageType}Min").ToList();
        _processDamage(stats, result, damageTypes, StatSection.Body);

        damageTypes = DamageTypeCatalog.BodyFields.Select(damageType => $"offensive{damageType}Modifier").ToList();
        _processDamage(stats, result, damageTypes, StatSection.Body);

    }

    private void _processBodyRetaliation(ISet<StatValue> stats, List<FormattedStat> result)
    {
        var damageTypes = DamageTypeCatalog.BodyFields.Select(damageType => $"retaliation{damageType}Min").ToList();

        var candidates = stats.Where(stat => damageTypes.Contains(stat.Field));

        foreach (var minimumDamage in candidates)
        {
            var damageType = minimumDamage.Field
                .Replace("retaliation", "", StringComparison.Ordinal)
                .Replace("Min", "", StringComparison.Ordinal);
            var maximumDamage = stats.FirstOrDefault(stat => stat.Field.Equals($"retaliation{damageType}Max", StringComparison.Ordinal));
            var duration = stats.FirstOrDefault(stat => stat.Field.Equals($"retaliation{damageType}DurationMin", StringComparison.Ordinal));

            var minimumDamageValue = minimumDamage.Value;

            var textBuilder = new StringBuilder();

            if (maximumDamage != null)
            {
                textBuilder.Append(_tags.GetTag("customtag_013_retaliation"));
            }
            else
            {
                textBuilder.Append(_tags.GetTag("customtag_03_retaliation"));
            }

            if (duration != null)
            {
                textBuilder.Append(_tags.GetTag("customtag_retaliation_delay"));
                minimumDamageValue *= duration.Value;
            }

            result.Add(new FormattedStat
            {
                Text = textBuilder.ToString(),
                Param0 = minimumDamageValue,
                Param1 = maximumDamage?.Value,
                Param3 = _resolveDamageType(damageType),
                Param4 = duration?.Value,
                Type = StatSection.Body
            });
        }
    }

    private void _processDamage(
        ISet<StatValue> stats,
        List<FormattedStat> result,
        List<string> damageTypes,
        StatSection section)
    {
        var candidates = stats.Where(stat => damageTypes.Contains(stat.Field));

        foreach (var minimumDamage in candidates)
        {
            var damageType = minimumDamage.Field
                .Replace("offensive", "", StringComparison.Ordinal)
                .Replace("Min", "", StringComparison.Ordinal);
            var maximumDamage = stats.FirstOrDefault(stat => stat.Field.Equals($"offensive{damageType}Max", StringComparison.Ordinal));
            var chance = stats.FirstOrDefault(stat => stat.Field.Equals($"offensive{damageType}Chance", StringComparison.Ordinal));
            var duration = stats.FirstOrDefault(stat => stat.Field.Equals($"offensive{damageType}DurationMin", StringComparison.Ordinal));

            var minimumDamageValue = minimumDamage.Value;

            var textBuilder = new StringBuilder();

            if (chance != null)
            {
                textBuilder.Append(_tags.GetTag("customtag_damage_chanceof"));
            }

            if (maximumDamage != null)
            {
                textBuilder.Append(_tags.GetTag("customtag_damage_123"));
            }
            else
            {
                textBuilder.Append(damageType.Contains("Modifier", StringComparison.Ordinal)
                    ? _tags.GetTag("customtag_damage_13%")
                    : _tags.GetTag("customtag_damage_13"));
            }

            if (duration != null)
            {
                textBuilder.Append(_tags.GetTag("customtag_damage_delay"));
                minimumDamageValue *= duration.Value;
            }

            var sm = new FormattedStat
            {
                Text = textBuilder.ToString(),
                Param0 = chance?.Value,
                Param1 = minimumDamageValue,
                Param2 = maximumDamage?.Value,
                Param3 = _resolveDamageType(damageType),
                Param4 = duration?.Value,
                Type = section
            };

            result.Add(sm);
        }
    }

    private void _mapSimpleHeaderEntries(ISet<StatValue> stats, List<FormattedStat> result)
    {
        string[] tags = {
                "offensivePierceRatioMin",
                "defensiveProtection",
                "skillChanceWeight",
                "skillProjectileNumber",
                "skillCooldownTime",
                "skillManaCost",
                "skillTargetRadius",
                "skillActiveDuration"
            };

        var headerTranslationTable = new Dictionary<string, string>();

        foreach (var tag in tags)
        {
            headerTranslationTable[tag] = _tags.GetTag(tag);
        }

        foreach (var stat in stats.Where(stat => headerTranslationTable.ContainsKey(stat.Field)))
        {
            result.Add(new FormattedStat
            {
                Text = headerTranslationTable[stat.Field],
                Param0 = stat.Value
            });
        }
    }



    private void _mapSimpleBodyEntries(ISet<StatValue> stats, List<FormattedStat> result)
    {
        string[] tags = {
                "defensiveAllMaxResist",
                "weaponDamagePct",
                "offensivePercentCurrentLifeMin",
                "characterLife",
                "characterLifeModifier",
                "augmentAllLevel",
                "characterDefensiveAbility",
                "characterOffensiveAbility",
                "characterDefensiveAbilityModifier",
                "characterOffensiveAbilityModifier",
                "defensiveBlockModifier",
                "defensivePetrify",
                "offensiveCritDamageModifier",
                "characterRunSpeedModifier",
                "characterIncreasedExperience",
                "characterIntelligenceModifier",
                "skillCooldownReduction",
                "retaliationTotalDamageModifier",
                "characterAttackSpeedModifier",
                "defensiveFreeze",
                "characterAttackSpeed",
                "offensiveLifeLeechMin",
                "characterIntelligence",
                "characterManaRegen",
                "characterManaRegenModifier",
                "characterLightRadius",
                "characterDodgePercent",
                "piercingProjectile",
                "characterMana",
                "characterManaModifier",
                "characterEnergyAbsorptionPercent",
                "characterSpellCastSpeedModifier",
                "defensiveReflect",
                "blockRecoveryTime", "characterLifeRegen",
                "characterDexterity",
                "characterDexterityModifier",
                "defensiveTrap",
                "characterLifeRegenModifier",
                "characterDeflectProjectile",
                "characterConstitutionModifier",
                "characterHuntingDexterityReqReduction",
                "characterStrength",
                "characterStrengthModifier",
                "characterTotalSpeedModifier",
                "skillProjectileSpeedModifier",
                "defensiveAbsorptionModifier",
                "skillLifeBonus",
                "skillLifePercent",
                "defensiveTotalSpeedResistance",
                "offensiveTauntMin",
                "defensiveBlockAmountModifier",
                "characterDefensiveBlockRecoveryReduction",
                "defensiveProtectionModifier",
                "projectilePiercingChance",
                "skillProjectileNumber",
                "projectileExplosionRadius",
                "defensiveStun",
                "offensiveTotalDamageModifier",
                "characterGlobalReqReduction"
            };

        var translationTable = new Dictionary<string, string>();

        foreach (var tag in tags)
        {
            translationTable[tag] = _tags.GetTag(tag);
        }

        var damageTypes = DamageTypeCatalog.BodyFields;

        foreach (var damageType in damageTypes)
        {
            translationTable[$"defensive{damageType}"] = _tags.GetTag($"defensive{damageType}");
            translationTable[$"defensive{damageType}Resistance"] = _tags.GetTag($"defensive{damageType}Resistance");
            translationTable[$"defensive{damageType}MaxResist"] = _tags.GetTag($"defensive{damageType}MaxResist");
        }

        foreach (var stat in stats.Where(stat => translationTable.ContainsKey(stat.Field)))
        {
            result.Add(new FormattedStat
            {
                Text = translationTable[stat.Field],
                Param0 = (float)Math.Round(stat.Value, 1, MidpointRounding.AwayFromZero),
                Param3 = stat.TextValue
            });
        }
    }



    public List<FormattedStat> ProcessStats(ISet<StatValue> stats, StatSection section)
    {
        var result = new List<FormattedStat>();

        switch (section)
        {
            case StatSection.Body:
                _processBodyDamage(stats, result);
                _processBodyRetaliation(stats, result);
                _mapSimpleBodyEntries(stats, result);
                _processRacialBonuses(stats, result);
                _processConversionDamage(stats, result);
                _processReducedResistances(stats, result);
                _processFactionWrits(stats, result);
                _processAddMastery(stats, result);

                _processStun(stats, result);
                _processAngleDamage(stats, result);
                _processChainDamage(stats, result);
                _processSlowRetaliation(stats, result);
                _processAddSkill(stats, result);

                break;

            case StatSection.Header:
                _processShieldBlock(stats, result);
                _processHeaderDamage(stats, result);
                _mapSimpleHeaderEntries(stats, result);
                _processAttackSpeed(stats, result);

                break;

            case StatSection.Pet:
                {
                    // In earlier preprocessing the pet stats were prefixed with "pet"
                    var petResults = new List<FormattedStat>();

                    var petStats = new HashSet<StatValue>(stats
                        .Where(stat => stat.Field.StartsWith("pet", StringComparison.Ordinal) && stat.Field != "petBonusName")
                        .Select(stat => new StatValue
                        {
                            Field = stat.Field.Remove(0, 3),
                            TextValue = stat.TextValue,
                            Value = stat.Value,
                        }));

                    _processShieldBlock(petStats, petResults);
                    _processHeaderDamage(petStats, petResults);
                    _processBodyDamage(petStats, petResults);
                    _processBodyRetaliation(petStats, petResults);
                    _mapSimpleHeaderEntries(petStats, petResults);
                    _mapSimpleBodyEntries(petStats, petResults);
                    _processRacialBonuses(petStats, petResults);
                    _processConversionDamage(petStats, petResults);
                    _processReducedResistances(petStats, petResults);

                    _processStun(petStats, petResults);
                    _processAngleDamage(petStats, petResults);
                    _processChainDamage(petStats, petResults);

                    result.AddRange(petResults.Select(stat =>
                    {
                        stat.Type = StatSection.Pet;

                        return stat;
                    }));
                }

                break;

        }

        return result;
    }

    [GeneratedRegex(@"\[ms\](.*)\[fs\](.*)", RegexOptions.CultureInvariant)]
    private static partial Regex _classNamePattern();
}
