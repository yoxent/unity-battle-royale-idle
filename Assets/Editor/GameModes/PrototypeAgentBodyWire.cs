using BattleRoyale.GameModes.BattleRoyale;
using KevinIglesias;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

namespace BattleRoyale.Editor.GameModes
{
    public static class PrototypeAgentBodyWire
    {
        const string AgentPath = "Assets/GameModes/BattleRoyale/PrototypeAgent.prefab";
        const string BodyPath =
            "Assets/Art/Kevin Iglesias/Human Animations/Unity Demo Scenes/Human Soldier Animations/Prefabs/SoldierRifleM.prefab";
        const string BodyName = "SoldierRifleM";
        const string RetiredBodyName = "Mannequin_Medium";

        [InitializeOnLoadMethod]
        static void WireAfterLoad()
        {
            EditorApplication.delayCall += Wire;
        }

        public static void Wire()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode ||
                ShowsCapsule() ||
                AlreadyWired())
            {
                return;
            }

            var bodyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BodyPath);
            if (bodyPrefab == null)
            {
                return;
            }

            var root = PrefabUtility.LoadPrefabContents(AgentPath);
            try
            {
                DestroyChild(root.transform, "AgentCapsule");
                DestroyChild(root.transform, RetiredBodyName);

                var visual = FindChild(root.transform, BodyName);
                if (visual == null)
                {
                    var instance = (GameObject)PrefabUtility.InstantiatePrefab(
                        bodyPrefab,
                        root.transform);
                    instance.transform.localPosition = Vector3.zero;
                    instance.transform.localRotation = Quaternion.identity;
                    instance.transform.localScale = Vector3.one;
                    visual = instance.transform;
                }

                var soldier = visual.GetComponent<HumanSoldierController>();
                if (soldier == null)
                {
                    Debug.LogError("SoldierRifleM is missing HumanSoldierController.");
                    return;
                }

                if (soldier.animator != null)
                {
                    soldier.animator.applyRootMotion = false;
                }

                HideGunCube(root.transform);

                var view = root.GetComponent<BattleRoyaleAgentBodyView>();
                if (view == null)
                {
                    view = root.AddComponent<BattleRoyaleAgentBodyView>();
                }

                var skinned = visual.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                var viewObject = new SerializedObject(view);
                viewObject.FindProperty("visual").objectReferenceValue = visual;
                viewObject.FindProperty("soldier").objectReferenceValue = soldier;
                viewObject.FindProperty("navigation").objectReferenceValue =
                    root.GetComponent<NavMeshAgent>();
                var rendererArray = viewObject.FindProperty("renderers");
                rendererArray.arraySize = skinned.Length;
                for (var i = 0; i < skinned.Length; i++)
                {
                    rendererArray.GetArrayElementAtIndex(i).objectReferenceValue = skinned[i];
                }

                viewObject.ApplyModifiedPropertiesWithoutUndo();

                var agentObject = new SerializedObject(root.GetComponent<BattleRoyaleAgent>());
                agentObject.FindProperty("bodyView").objectReferenceValue = view;
                agentObject.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, AgentPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        static bool ShowsCapsule()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AgentPath);
            if (prefab == null)
            {
                return false;
            }

            var capsule = prefab.transform.Find("BodyCapsule");
            if (capsule == null || !capsule.gameObject.activeSelf)
            {
                return false;
            }

            var soldier = prefab.transform.Find(BodyName);
            if (soldier != null && soldier.gameObject.activeSelf)
            {
                return false;
            }

            var gun = prefab.transform.Find("GunCube");
            var gunRenderer = gun == null ? null : gun.GetComponent<MeshRenderer>();
            return gunRenderer == null || !gunRenderer.enabled;
        }

        static bool AlreadyWired()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AgentPath);
            if (prefab == null || prefab.transform.Find("AgentCapsule") != null)
            {
                return prefab == null;
            }

            if (prefab.transform.Find(RetiredBodyName) != null)
            {
                return false;
            }

            var agent = prefab.GetComponent<BattleRoyaleAgent>();
            var view = prefab.GetComponent<BattleRoyaleAgentBodyView>();
            if (agent == null || view == null)
            {
                return false;
            }

            var assigned = new SerializedObject(agent)
                .FindProperty("bodyView")
                .objectReferenceValue == view;
            if (!assigned)
            {
                return false;
            }

            var viewObject = new SerializedObject(view);
            var visual = viewObject.FindProperty("visual").objectReferenceValue as Transform;
            var soldier = viewObject.FindProperty("soldier").objectReferenceValue as HumanSoldierController;
            if (visual == null || visual.name != BodyName || soldier == null || !soldier.enabled)
            {
                return false;
            }

            if (soldier.animator != null && soldier.animator.applyRootMotion)
            {
                return false;
            }

            var gun = prefab.transform.Find("GunCube");
            var gunRenderer = gun == null ? null : gun.GetComponent<MeshRenderer>();
            return gunRenderer == null || !gunRenderer.enabled;
        }

        static void HideGunCube(Transform root)
        {
            var gun = root.Find("GunCube");
            if (gun == null)
            {
                return;
            }

            var renderer = gun.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.enabled = false;
            }
        }

        static Transform FindChild(Transform root, string childName)
        {
            for (var i = 0; i < root.childCount; i++)
            {
                var child = root.GetChild(i);
                if (child.name == childName)
                {
                    return child;
                }
            }

            return null;
        }

        static void DestroyChild(Transform root, string childName)
        {
            var child = FindChild(root, childName);
            if (child != null)
            {
                Object.DestroyImmediate(child.gameObject);
            }
        }
    }
}
