using System.Collections.Generic;
using BattleRoyale.GameModes;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BattleRoyale.Maps.EditorTools
{
    public static class StatTrackerScenePlacement
    {
        public static void Sync(SimulationStage stage)
        {
            if (stage == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                stage.EnsureTrackers();
                return;
            }

            var serialized = new SerializedObject(stage);
            var slots = serialized.FindProperty("slots");
            if (slots == null)
            {
                return;
            }

            var changed = false;
            var count = Mathf.Min(slots.arraySize, SimulationStageLayout.SlotCount);
            for (var slot = 0; slot < count; slot++)
            {
                changed |= SyncSlot(stage, serialized, slots, slot);
            }

            if (changed)
            {
                serialized.ApplyModifiedProperties();
                EditorSceneManager.MarkSceneDirty(stage.gameObject.scene);
            }

            ClearPrefabOverrides(stage, slots, count);
            ApplyEditorVisibility(stage, slots, count);
        }

        static void ClearPrefabOverrides(
            SimulationStage stage,
            SerializedProperty slots,
            int count)
        {
            if (Application.isPlaying)
            {
                return;
            }

            for (var slot = 0; slot < count; slot++)
            {
                var entry = slots.GetArrayElementAtIndex(slot);
                var match = entry.FindPropertyRelative("match").objectReferenceValue as SimulationMatch;
                var tracker = entry.FindPropertyRelative("tracker").objectReferenceValue as StatSheetView;
                if (tracker == null || !PrefabUtility.IsPartOfPrefabInstance(tracker))
                {
                    continue;
                }

                var root = PrefabUtility.GetOutermostPrefabInstanceRoot(tracker.gameObject);
                root.SetActive(false);
                PrefabUtility.RevertPrefabInstance(root, InteractionMode.AutomatedAction);
                root.SetActive(false);
                if (root.transform.parent != stage.transform)
                {
                    root.transform.SetParent(stage.transform, false);
                }

                var trackerObject = new SerializedObject(tracker);
                var matchProperty = trackerObject.FindProperty("match");
                if (matchProperty != null)
                {
                    matchProperty.objectReferenceValue = match;
                    trackerObject.ApplyModifiedPropertiesWithoutUndo();
                }

                var source = PrefabUtility.GetCorrespondingObjectFromSource(root);
                var sourceTransform = source == null ? null : source.transform;
                var modifications = PrefabUtility.GetPropertyModifications(root);
                var kept = new List<PropertyModification>();
                for (var index = 0; index < modifications.Length; index++)
                {
                    var modification = modifications[index];
                    if (modification.propertyPath == "match" ||
                        modification.propertyPath == "m_Name" ||
                        modification.propertyPath == "m_IsActive" ||
                        modification.target == sourceTransform)
                    {
                        kept.Add(modification);
                    }
                }

                PrefabUtility.SetPropertyModifications(root, kept.ToArray());
            }
        }

        static void ApplyEditorVisibility(
            SimulationStage stage,
            SerializedProperty slots,
            int count)
        {
            if (Application.isPlaying)
            {
                return;
            }

            var dirty = false;
            for (var slot = 0; slot < count; slot++)
            {
                var tracker = slots.GetArrayElementAtIndex(slot)
                    .FindPropertyRelative("tracker").objectReferenceValue as StatSheetView;
                if (tracker == null)
                {
                    continue;
                }

                var visible = slot == stage.WatchedSlot;
                if (tracker.gameObject.activeSelf == visible)
                {
                    continue;
                }

                tracker.gameObject.SetActive(visible);
                dirty = true;
            }

            if (dirty)
            {
                EditorSceneManager.MarkSceneDirty(stage.gameObject.scene);
            }
        }

        static bool SyncSlot(
            SimulationStage stage,
            SerializedObject serialized,
            SerializedProperty slots,
            int slot)
        {
            var entry = slots.GetArrayElementAtIndex(slot);
            var match = entry.FindPropertyRelative("match").objectReferenceValue as SimulationMatch;
            var trackerProperty = entry.FindPropertyRelative("tracker");
            var current = trackerProperty.objectReferenceValue as StatSheetView;
            if (match == null)
            {
                return DestroyTracker(trackerProperty, current);
            }

            var prefab = stage.TrackerPrefabFor(match);
            if (current != null &&
                current.Matches(match) &&
                current.BoundMatch == match &&
                (prefab == null || SamePrefab(current, prefab)))
            {
                return false;
            }

            var removed = DestroyTracker(trackerProperty, current);
            if (prefab == null)
            {
                if (stage.TrackerPrefabFor(match) == null && HasAnyPrefab(stage))
                {
                    Debug.LogError(
                        $"Simulation slot {slot} has no stat tracker prefab for {match.GetType().Name}.",
                        stage);
                }

                return removed;
            }

            var instance = PrefabUtility.InstantiatePrefab(prefab.gameObject, stage.transform) as GameObject;
            if (instance == null)
            {
                Debug.LogError(
                    $"Simulation slot {slot} could not spawn its stat tracker.",
                    stage);
                return removed;
            }

            instance.name = prefab.name;
            var view = instance.GetComponent<StatSheetView>();
            if (view == null)
            {
                Object.DestroyImmediate(instance);
                Debug.LogError(
                    $"The stat tracker prefab for slot {slot} needs a StatSheetView on its root.",
                    prefab);
                return removed;
            }

            Undo.RegisterCreatedObjectUndo(instance, "Spawn Stat Tracker");
            view.Bind(match);
            trackerProperty.objectReferenceValue = view;
            serialized.ApplyModifiedProperties();
            return true;
        }

        static bool HasAnyPrefab(SimulationStage stage)
        {
            var prefabs = new SerializedObject(stage).FindProperty("trackerPrefabs");
            if (prefabs == null)
            {
                return false;
            }

            for (var index = 0; index < prefabs.arraySize; index++)
            {
                if (prefabs.GetArrayElementAtIndex(index).objectReferenceValue != null)
                {
                    return true;
                }
            }

            return false;
        }

        static bool SamePrefab(StatSheetView tracker, StatSheetView prefab)
        {
            if (tracker == null || prefab == null)
            {
                return false;
            }

            var source = PrefabUtility.GetCorrespondingObjectFromSource(tracker);
            return source == prefab;
        }

        static bool DestroyTracker(SerializedProperty trackerProperty, StatSheetView current)
        {
            if (current == null)
            {
                return false;
            }

            trackerProperty.objectReferenceValue = null;
            Undo.DestroyObjectImmediate(current.gameObject);
            return true;
        }
    }
}
