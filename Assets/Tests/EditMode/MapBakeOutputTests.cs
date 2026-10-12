using BattleRoyale.Maps;
using BattleRoyale.Maps.EditorTools;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace BattleRoyale.Tests.EditMode
{
    public sealed class MapBakeOutputTests
    {
        private const string Folder = "Assets/Tests/EditMode/TempMapBake";

        [Test]
        public void FloorImageMeshStaysOnTheBakedPrefab()
        {
            if (AssetDatabase.IsValidFolder(Folder))
            {
                AssetDatabase.DeleteAsset(Folder);
            }

            AssetDatabase.CreateFolder("Assets/Tests/EditMode", "TempMapBake");
            var definition = ScriptableObject.CreateInstance<MapDefinition>();
            definition.name = "TempMapBake_Map";
            AssetDatabase.CreateAsset(definition, Folder + "/TempMapBake_Map.asset");

            var root = new GameObject("Map");
            Undo.RegisterCreatedObjectUndo(root, "Generate Dungeon Map Root");
            var visual = new GameObject("VisualMap");
            visual.transform.SetParent(root.transform, false);
            var visualMesh = new Mesh { name = "TempMapBake_Map_Visual" };
            visualMesh.vertices = new[]
            {
                new Vector3(0f, 0f, 0f),
                new Vector3(0f, 0f, 1f),
                new Vector3(1f, 0f, 1f),
                new Vector3(1f, 0f, 0f)
            };
            visualMesh.uv = new[]
            {
                new Vector2(0f, 0f),
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                new Vector2(1f, 0f)
            };
            visualMesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
            visual.AddComponent<MeshFilter>().sharedMesh = visualMesh;
            visual.AddComponent<MeshRenderer>();

            var floor = new GameObject("DungeonFloor");
            floor.transform.SetParent(root.transform, false);
            var floorMesh = new Mesh { name = "TempMapBake_Map_DungeonFloor" };
            floorMesh.vertices = new[] { Vector3.zero, Vector3.forward, Vector3.right };
            floorMesh.triangles = new[] { 0, 1, 2 };
            var floorFilter = floor.AddComponent<MeshFilter>();
            floorFilter.sharedMesh = floorMesh;
            floor.AddComponent<MeshCollider>().sharedMesh = floorMesh;

            try
            {
                MapBakeOutput.Commit(root, definition, false);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                    Folder + "/TempMapBake_Map.prefab");
                Assert.NotNull(prefab);
                var bakedVisual = prefab.transform.Find("VisualMap").GetComponent<MeshFilter>();
                var bakedFloor = prefab.transform.Find("DungeonFloor").GetComponent<MeshFilter>();
                Assert.NotNull(bakedVisual.sharedMesh);
                Assert.AreEqual(4, bakedVisual.sharedMesh.vertexCount);
                Assert.NotNull(bakedFloor.sharedMesh);
                Assert.AreEqual(3, bakedFloor.sharedMesh.vertexCount);
            }
            finally
            {
                if (root != null)
                {
                    Object.DestroyImmediate(root);
                }

                AssetDatabase.DeleteAsset(Folder);
            }
        }
    }
}
