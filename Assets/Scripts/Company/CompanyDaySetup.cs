using System;
using BattleRoyale.GameModes;
using BattleRoyale.GameModes.BattleRoyale;
using UnityEngine;

namespace BattleRoyale.Company
{
    [Serializable]
    public struct AuthoredAgent
    {
        public BattleRoyaleAgentProfile archetype;
        public AgentRarity rarity;
        public int[] rolls;
        public int waivedAppearances;
    }

    [CreateAssetMenu(
        fileName = "CompanyDaySetup",
        menuName = "Battle Royale/Company/Day Setup")]
    public sealed class CompanyDaySetup : ScriptableObject
    {
        public string playerName = "Company";
        public TeamPlaystyle playerStance = TeamPlaystyle.Neutral;
        public FieldEmblem playerEmblem;
        public AuthoredAgent[] playerAgents;
        public string opponentName = "Ash Court";
        public TeamPlaystyle opponentStance = TeamPlaystyle.Defensive;
        public FieldEmblem opponentEmblem;
        public AuthoredAgent[] opponentAgents;
    }
}
