using UnityEngine;

namespace BattleRoyale.Company
{
    [CreateAssetMenu(
        fileName = "Rarity Colors",
        menuName = "Battle Royale/Company/Rarity Colors")]
    public sealed class AgentRarityColors : ScriptableObject
    {
        [SerializeField] Color[] colors;

        public bool TryGet(AgentRarity rarity, out Color color)
        {
            var index = (int)rarity;
            if (colors == null || index < 0 || index >= colors.Length)
            {
                color = default;
                return false;
            }

            color = colors[index];
            return true;
        }
    }
}
