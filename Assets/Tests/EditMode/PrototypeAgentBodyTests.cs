using BattleRoyale.Editor.GameModes;
using BattleRoyale.GameModes.BattleRoyale;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace BattleRoyale.Tests.EditMode
{
    public class PrototypeAgentBodyTests
    {
        [Test]
        public void PrototypeAgentShowsTheCapsuleAndKeepsTheSoldierInactive()
        {
            PrototypeAgentBodyWire.Wire();

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/GameModes/BattleRoyale/PrototypeAgent.prefab");
            Assert.IsNotNull(prefab);
            Assert.IsNull(prefab.transform.Find("AgentCapsule"));
            Assert.IsNull(prefab.transform.Find("Mannequin_Medium"));

            var capsule = prefab.transform.Find("BodyCapsule");
            Assert.IsNotNull(capsule);
            Assert.IsTrue(capsule.gameObject.activeSelf);

            var soldier = prefab.transform.Find("SoldierRifleM");
            Assert.IsNotNull(soldier);
            Assert.IsFalse(soldier.gameObject.activeSelf);

            var agent = prefab.GetComponent<BattleRoyaleAgent>();
            var view = prefab.GetComponent<BattleRoyaleAgentBodyView>();
            Assert.IsNotNull(view);
            var agentObject = new SerializedObject(agent);
            Assert.AreEqual(
                view,
                agentObject.FindProperty("bodyView").objectReferenceValue);

            var gun = prefab.transform.Find("GunCube");
            Assert.IsNotNull(gun);
            Assert.IsNotNull(gun.GetComponentInChildren<LineRenderer>(true));
            Assert.IsFalse(gun.GetComponent<MeshRenderer>().enabled);
        }
    }
}
