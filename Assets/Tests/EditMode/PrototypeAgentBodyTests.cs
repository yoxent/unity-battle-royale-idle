using BattleRoyale.Editor.GameModes;
using BattleRoyale.GameModes.BattleRoyale;
using KevinIglesias;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace BattleRoyale.Tests.EditMode
{
    public class PrototypeAgentBodyTests
    {
        [Test]
        public void PrototypeAgentUsesTheSoldierBody()
        {
            PrototypeAgentBodyWire.Wire();

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/GameModes/BattleRoyale/PrototypeAgent.prefab");
            Assert.IsNotNull(prefab);
            Assert.IsNull(prefab.transform.Find("AgentCapsule"));
            Assert.IsNull(prefab.transform.Find("Mannequin_Medium"));

            var agent = prefab.GetComponent<BattleRoyaleAgent>();
            var view = prefab.GetComponent<BattleRoyaleAgentBodyView>();
            Assert.IsNotNull(view);
            var agentObject = new SerializedObject(agent);
            Assert.AreEqual(
                view,
                agentObject.FindProperty("bodyView").objectReferenceValue);

            var viewObject = new SerializedObject(view);
            var visual = viewObject.FindProperty("visual").objectReferenceValue as Transform;
            Assert.IsNotNull(visual);
            Assert.AreEqual("SoldierRifleM", visual.name);

            var soldier = viewObject.FindProperty("soldier").objectReferenceValue as HumanSoldierController;
            Assert.IsNotNull(soldier);
            Assert.IsTrue(soldier.enabled);
            Assert.AreEqual(SoldierWeapons.Rifle, soldier.equippedWeapon);
            Assert.IsNotNull(soldier.animator);
            Assert.IsFalse(soldier.animator.applyRootMotion);
            Assert.Greater(viewObject.FindProperty("renderers").arraySize, 0);

            var gun = prefab.transform.Find("GunCube");
            Assert.IsNotNull(gun);
            Assert.IsNotNull(gun.GetComponentInChildren<LineRenderer>(true));
            Assert.IsFalse(gun.GetComponent<MeshRenderer>().enabled);
        }
    }
}
