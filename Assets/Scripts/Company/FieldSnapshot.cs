using BattleRoyale.GameModes;
using UnityEngine;

namespace BattleRoyale.Company
{
    [System.Serializable]
    public struct FieldColor
    {
        public float Red;
        public float Green;
        public float Blue;

        public Color AsColor()
        {
            return new Color(Red, Green, Blue, 1f);
        }
    }

    [System.Serializable]
    public struct FieldEmblem
    {
        public int InsigniaShape;
        public int Icon;
        public int FlagPattern;
        public FieldColor Primary;
        public FieldColor Secondary;
        public FieldColor Accent;
    }

    [System.Serializable]
    public struct FieldAgent
    {
        public string ArchetypeId;
        public AgentRarity Rarity;
        public int[] Rolls;
    }

    public struct FieldSnapshotSource
    {
        public string CompanyName;
        public FieldEmblem Emblem;
        public TeamPlaystyle Stance;
        public FieldAgent[] Agents;
        public int Balance;
        public int Upkeep;
    }

    public sealed class FieldSnapshot
    {
        public string CompanyName;
        public FieldEmblem Emblem;
        public TeamPlaystyle Stance;
        public FieldAgent[] Agents;
    }

    public static class FieldSnapshotRules
    {
        public static bool TryCapture(
            FieldSnapshotSource source,
            out FieldSnapshot snapshot)
        {
            snapshot = null;
            if (!IsFightRecord(source.CompanyName, source.Agents))
            {
                return false;
            }

            snapshot = Capture(source.CompanyName, source.Emblem, source.Stance, source.Agents);
            return true;
        }

        public static bool TryCopy(
            FieldSnapshot source,
            out FieldSnapshot copy)
        {
            copy = null;
            if (source == null || !IsFightRecord(source.CompanyName, source.Agents))
            {
                return false;
            }

            copy = Capture(source.CompanyName, source.Emblem, source.Stance, source.Agents);
            return true;
        }

        static FieldSnapshot Capture(
            string companyName,
            FieldEmblem emblem,
            TeamPlaystyle stance,
            FieldAgent[] agents)
        {
            return new FieldSnapshot
            {
                CompanyName = companyName,
                Emblem = emblem,
                Stance = stance,
                Agents = CopyAgents(agents),
            };
        }

        static bool IsFightRecord(string companyName, FieldAgent[] agents)
        {
            if (string.IsNullOrWhiteSpace(companyName) || agents == null)
            {
                return false;
            }

            for (var i = 0; i < agents.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(agents[i].ArchetypeId) ||
                    agents[i].Rolls == null ||
                    agents[i].Rolls.Length != AgentRollRules.StatCount)
                {
                    return false;
                }

                for (var stat = 0; stat < agents[i].Rolls.Length; stat++)
                {
                    var roll = agents[i].Rolls[stat];
                    if (roll < 1 || roll > 100)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        static FieldAgent[] CopyAgents(FieldAgent[] agents)
        {
            var copy = new FieldAgent[agents.Length];
            for (var i = 0; i < agents.Length; i++)
            {
                var rolls = new int[agents[i].Rolls.Length];
                for (var stat = 0; stat < rolls.Length; stat++)
                {
                    rolls[stat] = agents[i].Rolls[stat];
                }

                copy[i] = new FieldAgent
                {
                    ArchetypeId = agents[i].ArchetypeId,
                    Rarity = agents[i].Rarity,
                    Rolls = rolls,
                };
            }

            return copy;
        }
    }
}
