using System;
using BattleRoyale.GameModes;
using UnityEngine;

namespace BattleRoyale.GameModes.SoloBattleRoyale
{
    public enum BattleRoyaleAIStrategy
    {
        AggressiveHunter = 0,
        DefensiveAnchor = 1,
        NeutralOpportunist = 2,
        NeutralScout = 3,
        AggressiveSkirmisher = 4,
        DefensiveSurvivor = 5
    }

    public enum BattleRoyaleAgentState
    {
        Spawned,
        Exploring,
        SeekingTarget,
        Engaging,
        Retreating,
        MovingToSafeZone,
        Eliminated
    }

    public enum BattleRoyaleAgentSpecialty
    {
        Balanced = 0,
        LockdownDefender = 1,
        Athletic = 3,
        Sharpshooter = 4,
        Juggernaut = 5,
        ReconSpecialist = 6,
        Survivor = 7,
        Gunner = 8,
        Berserker = 9
    }

    [Serializable]
    public sealed class BattleRoyaleAgentStats
    {
        [Min(1f)]
        public float maxHealth = 100f;

        [Min(0f)]
        public float moveSpeed = 8f;

        [Min(0f)]
        public float perceptionRange = 80f;

        [Range(0f, 1f)]
        public float accuracy = 0.65f;

        [Range(0f, 1f)]
        public float awareness = 0.5f;

        [Range(0f, 1f)]
        public float resilience = 0.5f;

        [Range(0f, 1f)]
        public float athleticism = 0.5f;

        [Range(0f, 1f)]
        public float endurance = 0.5f;

        [Range(0f, 1f)]
        public float discipline = 0.5f;
    }

    [Serializable]
    public sealed class BattleRoyaleAISettings
    {
        [Range(0f, 1f)]
        public float aggression = 0.5f;

        [Range(0f, 1f)]
        public float retreatHealthPercent = 0.3f;

        [Min(0f)]
        public float preferredCombatRange = 20f;

        [Min(0.1f)]
        public float decisionInterval = 0.5f;

        [Min(0.1f)]
        public float wanderInterval = 3f;

        [Min(0f)]
        public float targetMemoryDuration = 1.5f;

        [Min(0f)]
        public float perceptionCacheDuration = 0.1f;

        [Min(0f)]
        public float targetClaimDuration = 1.5f;

        [Min(0f)]
        public float targetCommitmentDuration = 1f;

        [Range(0f, 1f)]
        public float targetSwitchScoreMargin = 0.15f;

        [Min(0.1f)]
        public float holdingPositionActivationTime = 3f;

        [Min(0f)]
        public float turnSpeed = 540f;
    }

    public readonly struct BattleRoyaleAgentSpecialtyEvaluation
    {
        public BattleRoyaleAgentSpecialtyEvaluation(
            BattleRoyaleAgentSpecialty specialty,
            float score)
        {
            Specialty = specialty;
            Score = score;
        }

        public BattleRoyaleAgentSpecialty Specialty { get; }
        public float Score { get; }
    }

    public sealed class BattleRoyaleSpecialtyModifiers
    {
        public float moveSpeedMultiplier = 1f;
        public float maxHealthMultiplier = 1f;
        public float damageTakenMultiplier = 1f;
        public float safeZoneDamageMultiplier = 1f;
        public float perceptionMultiplier = 1f;
        public float weaponRangeMultiplier = 1f;
        public float weaponDamageMultiplier = 1f;
        public float weaponFireRateMultiplier = 1f;
        public float accuracyBonus;
        public float holdingAccuracyBonus;
        public float holdingDamageTakenMultiplier = 1f;
        public float killBuffDuration;
        public float killBuffMoveSpeedMultiplier = 1f;
        public float killBuffDamageMultiplier = 1f;
        public float killBuffFireRateMultiplier = 1f;
        public float killBuffAccuracyBonus;
    }

    public static class BattleRoyaleAgentSpecialtyRules
    {
        public static BattleRoyaleAgentSpecialtyEvaluation Evaluate(
            BattleRoyaleAgentStats stats,
            BattleRoyaleAISettings ai = null)
        {
            if (stats == null)
            {
                return new BattleRoyaleAgentSpecialtyEvaluation(
                    BattleRoyaleAgentSpecialty.Balanced,
                    0f);
            }

            var health = Mathf.InverseLerp(75f, 150f, stats.maxHealth);
            var speed = Mathf.InverseLerp(5f, 12f, stats.moveSpeed);
            var perception = Mathf.InverseLerp(
                50f,
                120f,
                stats.perceptionRange);
            var bestSpecialty = BattleRoyaleAgentSpecialty.Balanced;
            var bestScore = 0.55f;

            Consider(
                BattleRoyaleAgentSpecialty.LockdownDefender,
                stats.resilience * 0.45f +
                stats.awareness * 0.3f +
                stats.discipline * 0.25f,
                ref bestSpecialty,
                ref bestScore);
            Consider(
                BattleRoyaleAgentSpecialty.Athletic,
                stats.athleticism * 0.45f +
                speed * 0.35f +
                stats.endurance * 0.2f,
                ref bestSpecialty,
                ref bestScore);
            Consider(
                BattleRoyaleAgentSpecialty.Sharpshooter,
                stats.accuracy * 0.55f +
                stats.awareness * 0.25f +
                stats.discipline * 0.2f,
                ref bestSpecialty,
                ref bestScore);
            Consider(
                BattleRoyaleAgentSpecialty.Gunner,
                stats.accuracy * 0.4f +
                stats.discipline * 0.35f +
                stats.endurance * 0.15f +
                stats.resilience * 0.1f,
                ref bestSpecialty,
                ref bestScore);
            Consider(
                BattleRoyaleAgentSpecialty.Juggernaut,
                health * 0.55f +
                stats.resilience * 0.3f +
                stats.endurance * 0.15f,
                ref bestSpecialty,
                ref bestScore);
            Consider(
                BattleRoyaleAgentSpecialty.ReconSpecialist,
                stats.awareness * 0.45f +
                perception * 0.35f +
                speed * 0.2f,
                ref bestSpecialty,
                ref bestScore);
            Consider(
                BattleRoyaleAgentSpecialty.Survivor,
                stats.endurance * 0.4f +
                stats.resilience * 0.35f +
                stats.awareness * 0.25f,
                ref bestSpecialty,
                ref bestScore);
            Consider(
                BattleRoyaleAgentSpecialty.Berserker,
                (ai == null ? 0.5f : ai.aggression) * 0.5f +
                stats.athleticism * 0.25f +
                stats.endurance * 0.25f,
                ref bestSpecialty,
                ref bestScore);

            return new BattleRoyaleAgentSpecialtyEvaluation(
                bestSpecialty,
                bestScore);
        }

        public static BattleRoyaleSpecialtyModifiers GetModifiers(
            BattleRoyaleAgentSpecialty specialty)
        {
            return specialty switch
            {
                BattleRoyaleAgentSpecialty.LockdownDefender =>
                    new BattleRoyaleSpecialtyModifiers
                    {
                        holdingAccuracyBonus = 0.15f,
                        holdingDamageTakenMultiplier = 0.8f
                    },
                BattleRoyaleAgentSpecialty.Athletic =>
                    new BattleRoyaleSpecialtyModifiers
                    {
                        moveSpeedMultiplier = 1.15f
                    },
                BattleRoyaleAgentSpecialty.Sharpshooter =>
                    new BattleRoyaleSpecialtyModifiers
                    {
                        accuracyBonus = 0.12f,
                        weaponRangeMultiplier = 1.15f
                    },
                BattleRoyaleAgentSpecialty.Juggernaut =>
                    new BattleRoyaleSpecialtyModifiers
                    {
                        maxHealthMultiplier = 1.2f,
                        damageTakenMultiplier = 0.8f,
                        moveSpeedMultiplier = 0.9f
                    },
                BattleRoyaleAgentSpecialty.ReconSpecialist =>
                    new BattleRoyaleSpecialtyModifiers
                    {
                        perceptionMultiplier = 1.25f,
                        weaponRangeMultiplier = 1.05f
                    },
                BattleRoyaleAgentSpecialty.Survivor =>
                    new BattleRoyaleSpecialtyModifiers
                    {
                        damageTakenMultiplier = 0.9f,
                        safeZoneDamageMultiplier = 0.65f
                    },
                BattleRoyaleAgentSpecialty.Gunner =>
                    new BattleRoyaleSpecialtyModifiers
                    {
                        weaponDamageMultiplier = 1.2f,
                        weaponFireRateMultiplier = 1.15f
                    },
                BattleRoyaleAgentSpecialty.Berserker =>
                    new BattleRoyaleSpecialtyModifiers
                    {
                        killBuffDuration = 5f,
                        killBuffMoveSpeedMultiplier = 1.15f,
                        killBuffDamageMultiplier = 1.25f,
                        killBuffFireRateMultiplier = 1.1f,
                        killBuffAccuracyBonus = 0.05f
                    },
                _ => new BattleRoyaleSpecialtyModifiers()
            };
        }

        public static string GetDisplayName(BattleRoyaleAgentSpecialty specialty)
        {
            return specialty switch
            {
                BattleRoyaleAgentSpecialty.LockdownDefender =>
                    "Lockdown Defender",
                BattleRoyaleAgentSpecialty.ReconSpecialist =>
                    "Recon Specialist",
                _ => specialty.ToString()
            };
        }

        private static void Consider(
            BattleRoyaleAgentSpecialty specialty,
            float score,
            ref BattleRoyaleAgentSpecialty bestSpecialty,
            ref float bestScore)
        {
            if (score <= bestScore)
            {
                return;
            }

            bestSpecialty = specialty;
            bestScore = score;
        }
    }

    public readonly struct BattleRoyaleStrategyTuning
    {
        public BattleRoyaleStrategyTuning(
            float aggression,
            float retreatHealthPercent,
            float preferredCombatRange,
            float perceptionMultiplier)
        {
            Aggression = aggression;
            RetreatHealthPercent = retreatHealthPercent;
            PreferredCombatRange = preferredCombatRange;
            PerceptionMultiplier = perceptionMultiplier;
        }

        public float Aggression { get; }
        public float RetreatHealthPercent { get; }
        public float PreferredCombatRange { get; }
        public float PerceptionMultiplier { get; }
    }

    public static class BattleRoyaleStrategyRules
    {
        public static BattleRoyaleStrategyTuning GetTuning(
            BattleRoyaleAIStrategy strategy,
            BattleRoyaleAISettings settings)
        {
            var baseAggression = Mathf.Clamp01(settings.aggression);
            var baseRetreat = Mathf.Clamp01(settings.retreatHealthPercent);
            var baseRange = Mathf.Max(0f, settings.preferredCombatRange);

            return strategy switch
            {
                BattleRoyaleAIStrategy.AggressiveHunter => new BattleRoyaleStrategyTuning(
                    Mathf.Clamp01(baseAggression + 0.35f),
                    Mathf.Clamp01(baseRetreat - 0.12f),
                    Mathf.Max(5f, baseRange * 0.65f),
                    1.25f),
                BattleRoyaleAIStrategy.AggressiveSkirmisher =>
                    new BattleRoyaleStrategyTuning(
                        Mathf.Clamp01(baseAggression + 0.25f),
                        Mathf.Clamp01(baseRetreat - 0.08f),
                        Mathf.Max(6f, baseRange * 0.9f),
                        1.35f),
                BattleRoyaleAIStrategy.NeutralScout =>
                    new BattleRoyaleStrategyTuning(
                        Mathf.Clamp01(baseAggression - 0.15f),
                        Mathf.Clamp01(baseRetreat + 0.05f),
                        Mathf.Max(15f, baseRange * 1.5f),
                        1.5f),
                BattleRoyaleAIStrategy.DefensiveAnchor =>
                    new BattleRoyaleStrategyTuning(
                        Mathf.Clamp01(baseAggression - 0.25f),
                        Mathf.Clamp01(baseRetreat + 0.12f),
                        Mathf.Max(12f, baseRange * 1.4f),
                        1.1f),
                BattleRoyaleAIStrategy.DefensiveSurvivor =>
                    new BattleRoyaleStrategyTuning(
                        Mathf.Clamp01(baseAggression - 0.35f),
                        Mathf.Clamp01(baseRetreat + 0.25f),
                        Mathf.Max(15f, baseRange * 1.6f),
                        1f),
                BattleRoyaleAIStrategy.NeutralOpportunist =>
                    new BattleRoyaleStrategyTuning(
                        baseAggression,
                        baseRetreat,
                        Mathf.Max(8f, baseRange),
                        1.1f),
                _ => new BattleRoyaleStrategyTuning(
                    baseAggression,
                    baseRetreat,
                    Mathf.Max(8f, baseRange),
                    1.1f)
            };
        }

        public static BattleRoyaleStrategyTuning GetTuning(
            BattleRoyaleAIStrategy strategy,
            BattleRoyaleAISettings settings,
            TeamPlaystyle teamPlaystyle)
        {
            var individualTuning = GetTuning(strategy, settings);
            var aggression = individualTuning.Aggression;
            var retreatHealthPercent = individualTuning.RetreatHealthPercent;
            var preferredCombatRange = individualTuning.PreferredCombatRange;
            var perceptionMultiplier = individualTuning.PerceptionMultiplier;

            switch (teamPlaystyle)
            {
                case TeamPlaystyle.Aggressive:
                    aggression += 0.2f;
                    retreatHealthPercent -= 0.1f;
                    preferredCombatRange *= 0.8f;
                    perceptionMultiplier *= 1.1f;
                    break;
                case TeamPlaystyle.Defensive:
                    aggression -= 0.2f;
                    retreatHealthPercent += 0.1f;
                    preferredCombatRange *= 1.25f;
                    perceptionMultiplier *= 0.9f;
                    break;
                case TeamPlaystyle.Neutral:
                default:
                    break;
            }

            return new BattleRoyaleStrategyTuning(
                Mathf.Clamp01(aggression),
                Mathf.Clamp01(retreatHealthPercent),
                Mathf.Max(2f, preferredCombatRange),
                Mathf.Max(0.1f, perceptionMultiplier));
        }
    }
}
