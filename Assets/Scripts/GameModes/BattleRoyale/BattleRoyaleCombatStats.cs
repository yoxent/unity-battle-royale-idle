using System;
using BattleRoyale.Company;

namespace BattleRoyale.GameModes.BattleRoyale
{
    public static class BattleRoyaleCombatStats
    {
        public static float MaxHealth(BattleRoyaleAgentStats stats)
        {
            return stats == null ? 0f : AgentStatRules.SharedHealth;
        }

        public static float MoveSpeed(
            BattleRoyaleAgentStats stats,
            BattleRoyaleSpecialtyModifiers modifiers,
            bool killBuff)
        {
            if (stats == null)
            {
                return 0f;
            }

            var speed = AgentStatRules.MoveSpeed(Roll(stats.athleticism));
            if (modifiers == null)
            {
                return speed;
            }

            speed *= modifiers.moveSpeedMultiplier;
            if (killBuff)
            {
                speed *= modifiers.killBuffMoveSpeedMultiplier;
            }

            return speed;
        }

        public static float Perception(
            BattleRoyaleAgentStats stats,
            BattleRoyaleSpecialtyModifiers modifiers)
        {
            if (stats == null)
            {
                return 0f;
            }

            var range = AgentStatRules.Perception(Roll(stats.awareness));
            if (modifiers != null)
            {
                range *= modifiers.perceptionMultiplier;
            }

            return range;
        }

        public static float BulletDamage(
            float incoming,
            BattleRoyaleAgentStats stats,
            float specialtyMultiplier)
        {
            var mitigated = AgentStatRules.BulletDamage(
                incoming,
                stats == null ? 0 : Roll(stats.resilience));
            if (specialtyMultiplier <= 0f)
            {
                return 0f;
            }

            return mitigated * specialtyMultiplier;
        }

        public static float HazardDamage(
            float incoming,
            BattleRoyaleAgentStats stats,
            float safeZoneMultiplier)
        {
            var mitigated = AgentStatRules.HazardDamage(
                incoming,
                stats == null ? 0 : Roll(stats.endurance));
            if (safeZoneMultiplier <= 0f)
            {
                return 0f;
            }

            return mitigated * safeZoneMultiplier;
        }

        public static int Roll(float unitRating)
        {
            if (float.IsNaN(unitRating) || unitRating <= 0f)
            {
                return 0;
            }

            if (unitRating >= 1f)
            {
                return 100;
            }

            return (int)Math.Round(
                unitRating * 100d,
                MidpointRounding.AwayFromZero);
        }
    }
}
