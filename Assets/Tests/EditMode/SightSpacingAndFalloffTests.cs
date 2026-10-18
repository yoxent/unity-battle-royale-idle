using BattleRoyale.GameModes.BattleRoyale;
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
        public void DisplayedGunRangeStopsAtAWall()
        {
            Assert.AreEqual(
                20f,
                BattleRoyaleLineOfSight.ClipDistance(20f, false, 4f));
            Assert.AreEqual(
                4f,
                BattleRoyaleLineOfSight.ClipDistance(20f, true, 4f));
            Assert.AreEqual(
                20f,
                BattleRoyaleLineOfSight.ClipDistance(20f, true, 30f));
        }

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
        public void BodyBottomSitsOnTheNavMeshPivot()
        {
            Assert.AreEqual(10f, BattleRoyaleAgent.CapsuleSeatOffset(10f));
            Assert.AreEqual(10f, BattleRoyaleAgent.CapsuleSeatOffset(-10f));
            Assert.AreEqual(
                10f,
                BattleRoyaleAgent.SeatedColliderCenter(Vector3.zero, 20f).y);
            Assert.AreEqual(15f, BattleRoyaleAgent.SeatedHeight(10f, 0.5f));
            Assert.AreEqual(17.5f, BattleRoyaleAgent.SeatedHeight(10f, 0.75f));
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
                "Assets/GameModes/BattleRoyale/PrototypeAgent.prefab");
            Assert.IsNotNull(prefab);

            var gun = prefab.transform.Find("GunCube");
            Assert.IsNotNull(gun);
            var line = gun.GetComponentInChildren<LineRenderer>(true);
            Assert.IsNotNull(line);
            Assert.IsNotNull(line.sharedMaterial);
            StringAssert.DoesNotContain(
                "Sprites/Default",
                line.sharedMaterial.shader.name);
            Assert.Greater(line.widthMultiplier, 0f);
        }

        [Test]
        public void SightLineWritesRangeOnLocalZAndLeavesXAlone()
        {
            var host = new GameObject("sight");
            try
            {
                host.transform.position = new Vector3(10f, 2f, -4f);
                var line = host.AddComponent<LineRenderer>();
                line.useWorldSpace = false;
                line.positionCount = 2;
                line.SetPosition(0, Vector3.zero);
                line.SetPosition(1, new Vector3(0f, 0f, 1f));
                var view = host.AddComponent<BattleRoyaleSightLines>();
                view.Bind(line);

                var start = host.transform.position;
                view.Show(0, start, start + new Vector3(3f, 1f, 12f));

                Assert.AreEqual(0f, line.GetPosition(0).x, 0.0001f);
                Assert.AreEqual(0f, line.GetPosition(1).x, 0.0001f);
                Assert.AreEqual(12f, line.GetPosition(1).z, 0.001f);
                Assert.AreEqual(new Vector3(10f, 2f, -4f), host.transform.position);
                Assert.AreEqual(Vector3.one, host.transform.localScale);
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void SightLineDefaultsLeaveTheMaterialAndWidthAlone()
        {
            var host = new GameObject("sight");
            try
            {
                var line = host.AddComponent<LineRenderer>();
                var material = line.sharedMaterial;
                line.widthMultiplier = 5f;
                BattleRoyaleSightLines.ApplyVisibleDefaults(line);

                Assert.AreEqual(material, line.sharedMaterial);
                Assert.AreEqual(5f, line.widthMultiplier);
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }
    }
}
