using System;
using UnityEngine;

namespace BattleRoyale.GameModes.SoloBattleRoyale
{
    public enum BattleRoyaleDirectiveType
    {
        None,
        Push,
        Hold,
        Retreat
    }

    public enum BattleRoyaleEngagementPolicy
    {
        Balanced,
        Aggressive,
        AvoidUnlessThreatened
    }

    [Serializable]
    public sealed class BattleRoyaleAgentDirective
    {
        public BattleRoyaleDirectiveType type;
        public Vector3 destination;
        public bool hasDestination;
        public BattleRoyaleEngagementPolicy engagementPolicy;
        public int revision;
    }

    public static class BattleRoyaleDirectiveRules
    {
        public static float GetPursuitRange(
            BattleRoyaleAgentDirective directive,
            float weaponRange,
            float perceptionRange,
            float baseAggression)
        {
            return BattleRoyaleAgentDecisionRules.GetPursuitRange(
                weaponRange,
                perceptionRange,
                GetPursuitAggression(directive, baseAggression));
        }

        public static float GetPursuitAggression(
            BattleRoyaleAgentDirective directive,
            float baseAggression)
        {
            var aggression = Mathf.Clamp01(baseAggression);
            if (directive == null)
            {
                return aggression;
            }

            switch (directive.type)
            {
                case BattleRoyaleDirectiveType.Push:
                    aggression = 1f;
                    break;
                case BattleRoyaleDirectiveType.Hold:
                case BattleRoyaleDirectiveType.Retreat:
                    aggression = 0f;
                    break;
            }

            return directive.engagementPolicy switch
            {
                BattleRoyaleEngagementPolicy.Aggressive =>
                    Mathf.Max(aggression, 0.75f),
                BattleRoyaleEngagementPolicy.AvoidUnlessThreatened =>
                    Mathf.Min(aggression, 0.25f),
                _ => aggression
            };
        }

        public static bool IsRetreat(
            BattleRoyaleAgentDirective directive)
        {
            return directive != null &&
                   directive.type == BattleRoyaleDirectiveType.Retreat;
        }

        public static bool IsHold(
            BattleRoyaleAgentDirective directive)
        {
            return directive != null &&
                   directive.type == BattleRoyaleDirectiveType.Hold;
        }

        public static bool HasDestination(
            BattleRoyaleAgentDirective directive)
        {
            return directive != null && directive.hasDestination;
        }
    }
}
