using System.Collections.Generic;
using UnityEngine;

namespace BattleRoyale.GameModes.SoloBattleRoyale
{
    [DisallowMultipleComponent]
    public sealed class BattleRoyaleProjectilePool : MonoBehaviour
    {
        [Min(1)]
        [SerializeField] private int initialCapacity = 64;

        [SerializeField] private BattleRoyaleProjectile bulletPrefab;

        private readonly Stack<BattleRoyaleProjectile> available = new();
        private bool prewarmed;
        private bool reportedMissingPrefab;

        public void Prewarm()
        {
            if (prewarmed)
            {
                return;
            }

            if (!ResolvePrefab())
            {
                return;
            }

            for (var index = 0; index < initialCapacity; index++)
            {
                available.Push(CreateProjectile());
            }

            prewarmed = true;
        }

        public void Spawn(
            BattleRoyaleAgent owner,
            Vector3 origin,
            Vector3 direction,
            float damage,
            float speed,
            float lifetime,
            float visualScale,
            float weaponRange)
        {
            if (!prewarmed)
            {
                Prewarm();
            }

            if (bulletPrefab == null)
            {
                return;
            }

            var projectile = available.Count > 0
                ? available.Pop()
                : CreateProjectile();
            projectile.gameObject.SetActive(true);
            projectile.Launch(
                owner,
                origin,
                direction,
                damage,
                speed,
                lifetime,
                visualScale,
                weaponRange);
        }

        public void Release(BattleRoyaleProjectile projectile)
        {
            if (projectile == null)
            {
                return;
            }

            projectile.transform.SetParent(transform, false);
            projectile.gameObject.SetActive(false);
            available.Push(projectile);
        }

        private bool ResolvePrefab()
        {
            if (bulletPrefab != null)
            {
                return true;
            }

            var catalog = CombatVfxCatalog.Load();
            bulletPrefab = catalog == null ? null : catalog.bullet;
            if (bulletPrefab != null)
            {
                return true;
            }

            if (reportedMissingPrefab)
            {
                return false;
            }

            reportedMissingPrefab = true;
            Debug.LogError(
                "Combat VFX is missing the bullet prefab.",
                this);
            return false;
        }

        private BattleRoyaleProjectile CreateProjectile()
        {
            var projectile = Instantiate(bulletPrefab, transform);
            projectile.Bind(this);
            projectile.gameObject.SetActive(false);
            return projectile;
        }
    }
}
