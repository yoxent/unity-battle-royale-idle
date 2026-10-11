using BattleRoyale.GameModes.SoloBattleRoyale;
using NUnit.Framework;
using UnityEngine;

namespace BattleRoyale.Tests.EditMode
{
    public sealed class CombatVfxTests
    {
        [Test]
        public void CatalogUsesBulletAndMuzzlePrefabsWithoutColliders()
        {
            var catalog = CombatVfxCatalog.Load();

            Assert.IsNotNull(catalog);
            Assert.IsNotNull(catalog.bullet);
            Assert.IsNotNull(catalog.muzzleFlash);
            Assert.IsNull(catalog.bullet.GetComponent<Collider>());
            Assert.IsNull(catalog.muzzleFlash.GetComponent<Collider>());
        }

        [Test]
        public void MuzzlePoolReusesPrewarmedFlashes()
        {
            var host = new GameObject("Muzzle Flashes")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            try
            {
                var pool = host.AddComponent<MuzzleFlashPool>();
                pool.Prewarm();
                var pooledCount = host.transform.childCount;

                pool.Show(Vector3.zero, 1f);
                pool.Show(Vector3.up, 1f);

                Assert.Greater(pooledCount, 1);
                Assert.AreEqual(pooledCount, host.transform.childCount);
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }
    }
}
