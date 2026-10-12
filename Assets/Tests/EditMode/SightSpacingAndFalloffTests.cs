using BattleRoyale.GameModes.SoloBattleRoyale;
using BattleRoyale.Weapons;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

namespace BattleRoyale.Tests.EditMode
{
    public sealed class SightSpacingAndFalloffTests
    {
        [Test]
        public void DamageStaysFullInsideGunRange()
        {
            Assert.AreEqual(20f, WeaponRangeFalloff.Scale(20f, 40f, 80f));
            Assert.AreEqual(1f, WeaponRangeFalloff.Multiplier(80f, 80f));
        }

        [Test]
        public void DamageDropsSharplyPastGunRange()
        {
            var atTwiceRange = WeaponRangeFalloff.Multiplier(160f, 80f);
            var farther = WeaponRangeFalloff.Multiplier(240f, 80f);

            Assert.Less(atTwiceRange, 0.1f);
            Assert.Less(farther, atTwiceRange);
        }

        [Test]
        public void LineOfSightReachesBeyondWeaponRange()
        {
            var fromObject = new GameObject("from");
            var toObject = new GameObject("to");
            try
            {
                var from = fromObject.AddComponent<BattleRoyaleAgent>();
                var to = toObject.AddComponent<BattleRoyaleAgent>();
                to.transform.position = new Vector3(0f, 0f, 500f);

                var clear = BattleRoyaleLineOfSight.IsClear(from, to);
                BattleRoyaleLineOfSight.Evaluate(
                    from,
                    to,
                    10f,
                    out _,
                    out var shortened);

                Assert.IsTrue(clear);
                Assert.Less(shortened.z, 20f);
            }
            finally
            {
                Object.DestroyImmediate(fromObject);
                Object.DestroyImmediate(toObject);
            }
        }

        [Test]
        public void NavigationRadiusMatchesBodyCollider()
        {
            var host = new GameObject("body");
            try
            {
                var agent = host.AddComponent<NavMeshAgent>();
                var body = host.AddComponent<CapsuleCollider>();
                body.radius = 5f;
                body.height = 20f;
                agent.radius = 0.5f;

                BattleRoyaleAgent.ApplyClearance(agent, body);

                Assert.AreEqual(5f, agent.radius);
                Assert.AreEqual(20f, agent.height);
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void OverlappingBodiesSeparateToTheirColliderEdges()
        {
            var push = BattleRoyaleAgent.SeparationOffset(
                Vector3.zero,
                5f,
                new Vector3(4f, 0f, 0f),
                5f,
                otherCanMove: true);

            Assert.AreEqual(-3f, push.x, 0.001f);
            Assert.AreEqual(0f, push.z, 0.001f);
        }

        [Test]
        public void AgentPrefabDrawsSightLineFromTheGun()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/GameModes/SoloBattleRoyale/PrototypeAgent.prefab");
            Assert.IsNotNull(prefab);

            var gun = prefab.transform.Find("GunCube");
            Assert.IsNotNull(gun);
            var line = gun.GetComponentInChildren<LineRenderer>(true);
            Assert.IsNotNull(line);
            Assert.IsNotNull(line.sharedMaterial);
            StringAssert.Contains(
                "Universal Render Pipeline",
                line.sharedMaterial.shader.name);
            StringAssert.DoesNotContain(
                "Sprites/Default",
                line.sharedMaterial.shader.name);
            Assert.GreaterOrEqual(line.widthMultiplier, 1f);
        }

        [Test]
        public void SightLineDefaultsDoNotUseTheSpriteShader()
        {
            var host = new GameObject("sight");
            try
            {
                var line = host.AddComponent<LineRenderer>();
                BattleRoyaleSightLines.ApplyVisibleDefaults(line);

                Assert.IsNotNull(line.sharedMaterial);
                StringAssert.Contains(
                    "Universal Render Pipeline",
                    line.sharedMaterial.shader.name);
                StringAssert.DoesNotContain(
                    "Sprites/Default",
                    line.sharedMaterial.shader.name);
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }
    }
}
