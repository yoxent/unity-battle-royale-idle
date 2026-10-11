using BattleRoyale.CameraSystems;
using BattleRoyale.Events;
using BattleRoyale.GameModes.SoloBattleRoyale;
using BattleRoyale.GameModes.TeamDeathmatch;
using BattleRoyale.Maps;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BattleRoyale.Editor.Maps
{
    [InitializeOnLoad]
    public static class ActiveMapEditorSync
    {
        static ActiveMapEditorSync()
        {
            EventBus.Subscribe<ActiveMapChanged>(SyncConsumers);
            EditorSceneManager.sceneOpened += OnSceneOpened;
            EditorApplication.delayCall += EnsureActiveMap;
        }

        public static void EnsureActiveMap()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode ||
                PrefabStageUtility.GetCurrentPrefabStage() != null)
            {
                return;
            }

            var mode = FindSceneComponent<SoloBattleRoyaleGameMode>();
            var teamMode = FindSceneComponent<TeamDeathmatchGameMode>();
            if (mode == null && teamMode == null)
            {
                return;
            }

            var manager = ActiveMapManager.Find();
            if (manager == null)
            {
                var managerObject = new GameObject("Active Map");
                Undo.RegisterCreatedObjectUndo(managerObject, "Create Active Map");
                manager = managerObject.AddComponent<ActiveMapManager>();
                var seed = mode != null ? mode.ActiveMap : teamMode.ActiveMap;
                if (seed != null)
                {
                    manager.SetActiveMap(seed);
                }

                EditorSceneManager.MarkSceneDirty(managerObject.scene);
            }

            if (manager.ActiveMap == null)
            {
                return;
            }

            AdoptSceneMap(mode, manager.ActiveMap);
            AdoptSceneMap(teamMode, manager.ActiveMap);

            foreach (var camera in Object.FindObjectsByType<SimulationCameraController>(
                         FindObjectsInactive.Include,
                         FindObjectsSortMode.None))
            {
                if ((camera.gameObject.hideFlags & HideFlags.HideAndDontSave) != 0)
                {
                    continue;
                }

                camera.SyncFromManager();
            }
        }

        private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            EnsureActiveMap();
        }

        private static void SyncConsumers(ActiveMapChanged change)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode ||
                change.Current == null ||
                change.CurrentIsTemporary)
            {
                return;
            }

            foreach (var mode in Object.FindObjectsByType<SoloBattleRoyaleGameMode>(
                         FindObjectsInactive.Include,
                         FindObjectsSortMode.None))
            {
                AdoptSceneMap(mode, change.Current);
            }

            foreach (var mode in Object.FindObjectsByType<TeamDeathmatchGameMode>(
                         FindObjectsInactive.Include,
                         FindObjectsSortMode.None))
            {
                AdoptSceneMap(mode, change.Current);
            }
        }

        private static void AdoptSceneMap(SoloBattleRoyaleGameMode mode, MapInstance map)
        {
            if (!CanAdopt(mode, mode != null && mode.ActiveMap == map))
            {
                return;
            }

            Undo.RecordObject(mode, "Sync Active Map");
            mode.AdoptActiveMap(map);
            EditorUtility.SetDirty(mode);
        }

        private static void AdoptSceneMap(TeamDeathmatchGameMode mode, MapInstance map)
        {
            if (!CanAdopt(mode, mode != null && mode.ActiveMap == map))
            {
                return;
            }

            Undo.RecordObject(mode, "Sync Active Map");
            mode.AdoptActiveMap(map);
            EditorUtility.SetDirty(mode);
        }

        private static bool CanAdopt(Component component, bool alreadyMatches)
        {
            return component != null &&
                   !alreadyMatches &&
                   (component.gameObject.hideFlags & HideFlags.HideAndDontSave) == 0 &&
                   component.gameObject.scene.IsValid();
        }

        private static T FindSceneComponent<T>() where T : Component
        {
            var components = Object.FindObjectsByType<T>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            foreach (var component in components)
            {
                if ((component.gameObject.hideFlags & HideFlags.HideAndDontSave) == 0 &&
                    component.gameObject.scene.IsValid() &&
                    component.gameObject.scene.isLoaded)
                {
                    return component;
                }
            }

            return null;
        }
    }
}
