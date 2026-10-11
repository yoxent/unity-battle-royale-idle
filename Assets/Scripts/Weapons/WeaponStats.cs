using System;
using UnityEngine;

namespace BattleRoyale.Weapons
{
    [Serializable]
    public sealed class WeaponStats
    {
        [Min(0f)]
        public float damage = 20f;

        [Min(0.01f)]
        public float fireRate = 1f;

        [Min(0f)]
        public float range = 80f;

        [Min(0.01f)]
        public float projectileSpeed = 60f;

        [Min(0.01f)]
        public float projectileLifetime = 3f;

        [Min(0.01f)]
        public float projectileScale = 0.35f;

        [Range(0f, 45f)]
        public float spreadDegrees = 2f;

        [Min(1)]
        public int pelletCount = 1;

        [Min(0.05f)]
        [Tooltip("Seconds to refill the magazine after the last round.")]
        public float reloadDuration = 2.5f;

        [Min(1)]
        [Tooltip("Rounds fired before the gun reloads.")]
        public int magazineSize = 30;
    }
}
