using BattleRoyale.Weapons;
using UnityEngine;

namespace BattleRoyale.GameModes.SoloBattleRoyale
{
    [CreateAssetMenu(
        fileName = "BattleRoyaleCharacterProfile",
        menuName = "Battle Royale/Characters/Character Profile")]
    public sealed class BattleRoyaleCharacterProfile : ScriptableObject
    {
        public string displayName = "Prototype Character";
        public BattleRoyaleCharacterStats stats = new();
        public BattleRoyaleAISettings ai = new();
        public WeaponProfile defaultWeapon;
        public BattleRoyaleAIStrategy defaultStrategy =
            BattleRoyaleAIStrategy.NeutralOpportunist;
        public float visualScale = 10f;
        public Color visualColor = Color.white;
    }
}
