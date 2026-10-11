using UnityEngine;

namespace BattleRoyale.GameModes.SoloBattleRoyale
{
    [CreateAssetMenu(
        fileName = "CombatVfx",
        menuName = "Battle Royale/Combat VFX")]
    public sealed class CombatVfxCatalog : ScriptableObject
    {
        public BattleRoyaleProjectile bullet;
        public PooledMuzzleFlash muzzleFlash;

        private static CombatVfxCatalog cached;

        public static CombatVfxCatalog Load()
        {
            if (cached == null)
            {
                cached = Resources.Load<CombatVfxCatalog>("CombatVfx");
            }

            return cached;
        }
    }
}
