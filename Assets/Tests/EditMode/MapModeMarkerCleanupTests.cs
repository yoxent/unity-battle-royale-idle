using BattleRoyale.Maps;
using BattleRoyale.Maps.EditorTools;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace BattleRoyale.Tests.EditMode
{
    public sealed class MapModeMarkerCleanupTests
    {
        private const string Folder = "Assets/Tests/EditMode/TempModeMarkers";

        [Test]
        public void DeselectingAModeRemovesOnlyThatModesMarkers()
        {
            var root = new GameObject("Map");
            var battleConfiguration = ScriptableObject.CreateInstance<MapModeConfiguration>();
            var deathmatchConfiguration = ScriptableObject.CreateInstance<MapModeConfiguration>();
            try
            {
                battleConfiguration.mode = MapGameMode.BattleRoyale;
                deathmatchConfiguration.mode = MapGameMode.TeamDeathmatch;

                var points = new GameObject(MapModeMarkerCleanup.MarkerRootName);
                points.transform.SetParent(root.transform, false);
                var misnamedBattle = CreateMarker(
                    points.transform,
                    "TeamDeathmatch_Misnamed",
                    battleConfiguration);
                var deathmatch = CreateMarker(
                    points.transform,
                    "BattleRoyale_ActuallyDeathmatch",
                    deathmatchConfiguration);
                var unnamedBattle = new GameObject("BattleRoyale_Solo");
                unnamedBattle.transform.SetParent(points.transform, false);

                MapModeMarkerCleanup.RemoveFromHierarchy(
                    root,
                    MapGameMode.BattleRoyale,
                    false);

                Assert.IsTrue(misnamedBattle == null);
                Assert.IsTrue(unnamedBattle == null);
                Assert.IsNotNull(deathmatch);
                Assert.IsNotNull(root.transform.Find(MapModeMarkerCleanup.MarkerRootName));

                MapModeMarkerCleanup.RemoveFromHierarchy(
                    root,
                    MapGameMode.TeamDeathmatch,
                    false);

                Assert.IsNull(root.transform.Find(MapModeMarkerCleanup.MarkerRootName));
            }
            finally
            {
                if (root != null)
                {
                    Object.DestroyImmediate(root);
                }

                Object.DestroyImmediate(battleConfiguration);
                Object.DestroyImmediate(deathmatchConfiguration);
            }
        }

        [Test]
        public void DeselectingAModeRemovesItsMarkersFromTheBakedPrefab()
        {
            if (AssetDatabase.IsValidFolder(Folder))
            {
                AssetDatabase.DeleteAsset(Folder);
            }

            AssetDatabase.CreateFolder("Assets/Tests/EditMode", "TempModeMarkers");
            var definition = ScriptableObject.CreateInstance<MapDefinition>();
            definition.name = "TempModeMarkers_Map";
            AssetDatabase.CreateAsset(definition, Folder + "/TempModeMarkers_Map.asset");

            var root = new GameObject("Map");
            var points = new GameObject(MapModeMarkerCleanup.MarkerRootName);
            points.transform.SetParent(root.transform, false);
            var battle = new GameObject("BattleRoyale_Solo");
            battle.transform.SetParent(points.transform, false);
            var deathmatch = new GameObject("TeamDeathmatch_Default");
            deathmatch.transform.SetParent(points.transform, false);

            try
            {
                var prefab = PrefabUtility.SaveAsPrefabAsset(
                    root,
                    Folder + "/TempModeMarkers_Map.prefab");
                definition.bakedPrefab = prefab;
                EditorUtility.SetDirty(definition);
                AssetDatabase.SaveAssets();
                Object.DestroyImmediate(root);
                root = null;

                MapModeMarkerCleanup.RemoveFromDefinition(
                    definition,
                    MapGameMode.BattleRoyale);

                var saved = AssetDatabase.LoadAssetAtPath<GameObject>(
                    Folder + "/TempModeMarkers_Map.prefab");
                Assert.IsNull(saved.transform.Find(
                    MapModeMarkerCleanup.MarkerRootName + "/BattleRoyale_Solo"));
                Assert.IsNotNull(saved.transform.Find(
                    MapModeMarkerCleanup.MarkerRootName + "/TeamDeathmatch_Default"));
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

        private static GameObject CreateMarker(
            Transform parent,
            string name,
            MapModeConfiguration configuration)
        {
            var markerObject = new GameObject(name);
            markerObject.transform.SetParent(parent, false);
            var point = new GameObject("Point");
            point.transform.SetParent(markerObject.transform, false);
            var marker = point.AddComponent<MapModePointMarker>();
            marker.configuration = configuration;
            return markerObject;
        }
    }
}
