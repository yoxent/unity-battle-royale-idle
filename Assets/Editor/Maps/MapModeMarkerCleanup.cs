using System;
using BattleRoyale.Maps;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BattleRoyale.Maps.EditorTools
{
    public static class MapModeMarkerCleanup
    {
        public const string MarkerRootName = "GameModePoints";

        public static void RemoveFromDefinition(MapDefinition mapDefinition, MapGameMode mode)
        {
            if (mapDefinition == null)
            {
                return;
            }

            RemoveFromPrefab(mapDefinition, mode);
            RemoveFromOpenScenes(mapDefinition, mode);
        }

        public static bool RemoveFromHierarchy(
            GameObject mapRoot,
            MapGameMode mode,
            bool recordUndo)
        {
            return RemoveFromHierarchy(mapRoot, mode, recordUndo, false);
        }

        private static void RemoveFromPrefab(MapDefinition mapDefinition, MapGameMode mode)
        {
            var prefab = mapDefinition.bakedPrefab;
            if (prefab == null)
            {
                return;
            }

            var path = AssetDatabase.GetAssetPath(prefab);
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            var stage = PrefabStageUtility.GetCurrentPrefabStage();
            if (stage != null && stage.assetPath == path)
            {
                if (RemoveFromHierarchy(stage.prefabContentsRoot, mode, false))
                {
                    EditorSceneManager.MarkSceneDirty(stage.scene);
                    PrefabUtility.SaveAsPrefabAsset(stage.prefabContentsRoot, path);
                }

                return;
            }

            var contents = PrefabUtility.LoadPrefabContents(path);
            try
            {
                if (RemoveFromHierarchy(contents, mode, false))
                {
                    PrefabUtility.SaveAsPrefabAsset(contents, path);
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        private static void RemoveFromOpenScenes(MapDefinition mapDefinition, MapGameMode mode)
        {
            foreach (var mapInstance in Resources.FindObjectsOfTypeAll<MapInstance>())
            {
                if (mapInstance == null ||
                    mapInstance.definition != mapDefinition ||
                    !mapInstance.gameObject.scene.IsValid() ||
                    !mapInstance.gameObject.scene.isLoaded)
                {
                    continue;
                }

                if (!RemoveFromHierarchy(mapInstance.gameObject, mode, true, true))
                {
                    continue;
                }

                EditorSceneManager.MarkSceneDirty(mapInstance.gameObject.scene);
            }
        }

        private static bool RemoveFromHierarchy(
            GameObject mapRoot,
            MapGameMode mode,
            bool recordUndo,
            bool skipPrefabOwned)
        {
            if (mapRoot == null)
            {
                return false;
            }

            var markerRoot = mapRoot.transform.Find(MarkerRootName);
            if (markerRoot == null)
            {
                return false;
            }

            var prefix = mode + "_";
            var removed = false;
            for (var childIndex = markerRoot.childCount - 1; childIndex >= 0; childIndex--)
            {
                var child = markerRoot.GetChild(childIndex);
                if (child == null ||
                    (skipPrefabOwned && IsPrefabOwned(child.gameObject)) ||
                    !BelongsToMode(child, mode, prefix))
                {
                    continue;
                }

                DestroyObject(child.gameObject, recordUndo);
                removed = true;
            }

            if (markerRoot.childCount > 0 ||
                (skipPrefabOwned && IsPrefabOwned(markerRoot.gameObject)))
            {
                return removed;
            }

            DestroyObject(markerRoot.gameObject, recordUndo);
            return true;
        }

        private static bool BelongsToMode(Transform configurationRoot, MapGameMode mode, string prefix)
        {
            var markers = configurationRoot.GetComponentsInChildren<MapModePointMarker>(true);
            var identified = false;
            for (var i = 0; i < markers.Length; i++)
            {
                var configuration = markers[i].configuration;
                if (configuration == null)
                {
                    continue;
                }

                identified = true;
                if (configuration.mode == mode)
                {
                    return true;
                }
            }

            if (identified)
            {
                return false;
            }

            return configurationRoot.name.StartsWith(prefix, StringComparison.Ordinal);
        }

        private static bool IsPrefabOwned(GameObject gameObject)
        {
            return PrefabUtility.IsPartOfPrefabInstance(gameObject) &&
                   PrefabUtility.GetCorrespondingObjectFromSource(gameObject) != null;
        }

        private static void DestroyObject(GameObject gameObject, bool recordUndo)
        {
            if (recordUndo)
            {
                Undo.DestroyObjectImmediate(gameObject);
                return;
            }

            UnityEngine.Object.DestroyImmediate(gameObject);
        }
    }
}
