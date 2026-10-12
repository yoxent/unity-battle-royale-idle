using BattleRoyale.Weapons;
using UnityEngine;

namespace BattleRoyale.GameModes.SoloBattleRoyale
{
    [CreateAssetMenu(
        fileName = "BattleRoyaleAgentProfile",
        menuName = "Battle Royale/Agents/Agent Profile")]
    public sealed class BattleRoyaleAgentProfile : ScriptableObject
    {
        public string displayName = "Prototype Agent";
        public BattleRoyaleAgentStats stats = new();
        public BattleRoyaleAISettings ai = new();
        public WeaponProfile defaultWeapon;
        public BattleRoyaleAIStrategy defaultStrategy =
            BattleRoyaleAIStrategy.NeutralOpportunist;
        public float visualScale = 10f;
        public Color visualColor = Color.white;
    }
}
