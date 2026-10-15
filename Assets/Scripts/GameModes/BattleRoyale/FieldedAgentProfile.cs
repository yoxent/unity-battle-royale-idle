using BattleRoyale.Company;
using UnityEngine;

namespace BattleRoyale.GameModes.BattleRoyale
{
    public static class FieldedAgentProfile
    {
        public static void WriteRolls(BattleRoyaleAgentStats stats, int[] rolls)
        {
            stats.accuracy = rolls[0] / 100f;
            stats.awareness = rolls[1] / 100f;
            stats.resilience = rolls[2] / 100f;
            stats.athleticism = rolls[3] / 100f;
            stats.endurance = rolls[4] / 100f;
            stats.discipline = rolls[5] / 100f;
        }

        public static BattleRoyaleAgentProfile Create(
            BattleRoyaleAgentProfile archetype,
            int[] rolls)
        {
            if (archetype == null || !HasRolls(rolls))
            {
                return null;
            }

            var profile = ScriptableObject.CreateInstance<BattleRoyaleAgentProfile>();
            profile.hideFlags = HideFlags.HideAndDontSave;
            profile.displayName = archetype.displayName;
            profile.ai = archetype.ai;
            profile.defaultWeapon = archetype.defaultWeapon;
            profile.defaultStrategy = archetype.defaultStrategy;
            profile.visualScale = archetype.visualScale;
            profile.visualColor = archetype.visualColor;
            profile.stats = new BattleRoyaleAgentStats();
            WriteRolls(profile.stats, rolls);
            return profile;
        }

        public static bool HasRolls(int[] rolls)
        {
            if (rolls == null || rolls.Length != AgentRollRules.StatCount)
            {
                return false;
            }

            for (var i = 0; i < rolls.Length; i++)
            {
                if (rolls[i] < 1 || rolls[i] > 100)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
