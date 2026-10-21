using System.Collections.Generic;
using BattleRoyale.GameModes;
using BattleRoyale.GameModes.BattleRoyale;
using BattleRoyale.GameModes.Deathmatch;
using BattleRoyale.GameModes.Elimination;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

namespace BattleRoyale.Maps.EditorTools
{
    public static class MapBakeOutput
    {
        public static string GetPrefabPath(MapDefinition definition)
        {
            if (definition == null)
            {
                return null;
            }

            var definitionPath = AssetDatabase.GetAssetPath(definition);
            if (string.IsNullOrEmpty(definitionPath))
            {
                return null;
            }

            var folder = System.IO.Path.GetDirectoryName(definitionPath)?.Replace('\\', '/');
            if (string.IsNullOrEmpty(folder))
            {
                return null;
            }

            return $"{folder}/{definition.name}.prefab";
        }

        public static bool UseModeOnSlot(SimulationStage stage, int slot, MapGameMode mode)
        {
            if (stage == null)
            {
                return false;
            }

            var stageObject = new SerializedObject(stage);
            var slots = stageObject.FindProperty("slots");
            if (slots == null || slot < 0 || slot >= slots.arraySize)
            {
                return false;
            }

            var matchProperty = slots.GetArrayElementAtIndex(slot).FindPropertyRelative("match");
            var current = matchProperty.objectReferenceValue as SimulationMatch;
            if (current == null)
            {
                return false;
            }

            if (current.Runs(mode))
            {
                return true;
            }

            var replacement = CreateMatch(current.gameObject, mode);
            if (replacement == null)
            {
                return false;
            }

            CopyMatchSetup(current, replacement);
            replacement.gameObject.name = ObjectNames.NicifyVariableName(mode.ToString());
            if (Application.isPlaying)
            {
                Object.DestroyImmediate(current);
            }
            else
            {
                Undo.DestroyObjectImmediate(current);
            }

            matchProperty.objectReferenceValue = replacement;
            stageObject.ApplyModifiedProperties();
            return true;
        }

        private static SimulationMatch CreateMatch(GameObject host, MapGameMode mode)
        {
            switch (mode)
            {
                case MapGameMode.Deathmatch:
                    return AddMatch<DeathmatchGameMode>(host);
                case MapGameMode.BattleRoyale:
                    return AddMatch<BattleRoyaleGameMode>(host);
                case MapGameMode.Elimination:
                    return AddMatch<EliminationGameMode>(host);
                default:
                    Debug.LogError($"No match component exists for {mode}.");
                    return null;
            }
        }

        private static SimulationMatch AddMatch<T>(GameObject host) where T : SimulationMatch
        {
            return Application.isPlaying
                ? host.AddComponent<T>()
                : Undo.AddComponent<T>(host);
        }

        private static void CopyMatchSetup(SimulationMatch from, SimulationMatch to)
        {
            var source = new SerializedObject(from);
            var destination = new SerializedObject(to);
            var property = source.GetIterator();
            if (!property.NextVisible(true))
            {
                return;
            }

            do
            {
                if (property.name.StartsWith("m_"))
                {
                    continue;
                }

                destination.CopyFromSerializedProperty(property);
            }
            while (property.NextVisible(false));

            destination.ApplyModifiedPropertiesWithoutUndo();
        }

        public static MapInstance Commit(
            GameObject root,
            MapDefinition definition,
            bool placeInScene)
        {
            if (root == null || definition == null)
            {
                return null;
            }

            var path = GetPrefabPath(definition);
            if (string.IsNullOrEmpty(path))
            {
                Debug.LogError(
                    "Save the map definition asset before generating its prefab.",
                    definition);
                return null;
            }

            root.name = "Map";
            var meshSlots = CaptureMeshSlots(root);
            var navigationCopy = CloneNavigation(root);
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            if (prefab == null)
            {
                var destroyed = new HashSet<Mesh>();
                foreach (var slot in meshSlots)
                {
                    if (slot.Copy != null && destroyed.Add(slot.Copy))
                    {
                        Object.DestroyImmediate(slot.Copy);
                    }
                }

                if (navigationCopy != null)
                {
                    Object.DestroyImmediate(navigationCopy);
                }

                Debug.LogError($"Could not save the map prefab at {path}.", definition);
                return null;
            }

            PersistGeneratedContent(prefab, meshSlots, navigationCopy);
            Undo.RecordObject(definition, "Assign Map Prefab");
            definition.bakedPrefab = prefab;
            EditorUtility.SetDirty(definition);
            AssetDatabase.SaveAssets();
            Undo.DestroyObjectImmediate(root);

            MapInstance placed = null;
            if (placeInScene)
            {
                placed = PlacePrefabInScene(prefab);
            }
            else
            {
                Selection.activeObject = prefab;
            }

            SaveDirtyScenesWithPaths();
            return placed;
        }

        private static void SaveDirtyScenesWithPaths()
        {
            var sceneCount = EditorSceneManager.sceneCount;
            for (var i = 0; i < sceneCount; i++)
            {
                var scene = EditorSceneManager.GetSceneAt(i);
                if (!scene.IsValid() || !scene.isDirty || string.IsNullOrEmpty(scene.path))
                {
                    continue;
                }

                EditorSceneManager.SaveScene(scene);
            }
        }

        public static MapInstance PlacePrefabInScene(GameObject prefab)
        {
            if (prefab == null || !prefab.TryGetComponent<MapInstance>(out _))
            {
                Debug.LogError("A map prefab needs a MapInstance on its root.", prefab);
                return null;
            }

            var manager = FindOrCreateManager();
            var instance = FindSceneInstance(prefab);
            if (instance == null)
            {
                instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                Undo.RegisterCreatedObjectUndo(instance, "Place Map Prefab");
            }

            instance.name = "Map";
            RemoveOtherMapInstances(instance);

            var map = instance.GetComponent<MapInstance>();
            var serializedManager = new SerializedObject(manager);
            serializedManager.FindProperty("activeMapPrefab").objectReferenceValue = prefab;
            serializedManager.ApplyModifiedProperties();
            Undo.RecordObject(manager, "Set Active Map");
            manager.SetActiveMap(map);
            EditorUtility.SetDirty(manager);
            EditorSceneManager.MarkSceneDirty(instance.scene);
            Selection.activeGameObject = instance;
            return map;
        }

        private readonly struct GeneratedMeshSlot
        {
            public GeneratedMeshSlot(string path, string objectName, bool collider, Mesh copy)
            {
                Path = path;
                ObjectName = objectName;
                Collider = collider;
                Copy = copy;
            }

            public string Path { get; }
            public string ObjectName { get; }
            public bool Collider { get; }
            public Mesh Copy { get; }
        }

        private static List<GeneratedMeshSlot> CaptureMeshSlots(GameObject root)
        {
            var slots = new List<GeneratedMeshSlot>();
            var copies = new Dictionary<Mesh, Mesh>();
            foreach (var filter in root.GetComponentsInChildren<MeshFilter>(true))
            {
                slots.Add(CreateMeshSlot(root.transform, filter.transform, false, filter.sharedMesh, copies));
            }

            foreach (var collider in root.GetComponentsInChildren<MeshCollider>(true))
            {
                slots.Add(CreateMeshSlot(root.transform, collider.transform, true, collider.sharedMesh, copies));
            }

            return slots;
        }

        private static GeneratedMeshSlot CreateMeshSlot(
            Transform root,
            Transform transform,
            bool collider,
            Mesh mesh,
            Dictionary<Mesh, Mesh> copies)
        {
            Mesh copy = null;
            if (mesh != null && !IsReusableAsset(mesh))
            {
                if (string.IsNullOrEmpty(mesh.name))
                {
                    mesh.name = transform.name;
                }

                if (!copies.TryGetValue(mesh, out copy))
                {
                    copy = Object.Instantiate(mesh);
                    copy.name = mesh.name;
                    copy.hideFlags = HideFlags.None;
                    copies.Add(mesh, copy);
                }
            }

            return new GeneratedMeshSlot(
                IndexedPath(root, transform),
                transform.name,
                collider,
                copy);
        }

        private static void PersistGeneratedContent(
            GameObject prefab,
            List<GeneratedMeshSlot> meshSlots,
            NavMeshData navigationCopy)
        {
            var added = new HashSet<Mesh>();
            foreach (var slot in meshSlots)
            {
                if (slot.Copy == null || !added.Add(slot.Copy))
                {
                    continue;
                }

                AssetDatabase.AddObjectToAsset(slot.Copy, prefab);
            }

            if (navigationCopy != null)
            {
                AssetDatabase.AddObjectToAsset(navigationCopy, prefab);
            }

            var prefabTransforms = IndexTransforms(prefab.transform);
            foreach (var slot in meshSlots)
            {
                if (slot.Copy == null ||
                    !prefabTransforms.TryGetValue(slot.Path, out var prefabTransform))
                {
                    continue;
                }

                if (slot.Collider)
                {
                    var collider = prefabTransform.GetComponent<MeshCollider>();
                    if (collider == null)
                    {
                        continue;
                    }

                    collider.sharedMesh = slot.Copy;
                    EditorUtility.SetDirty(collider);
                }
                else
                {
                    var filter = prefabTransform.GetComponent<MeshFilter>();
                    if (filter == null)
                    {
                        continue;
                    }

                    filter.sharedMesh = slot.Copy;
                    EditorUtility.SetDirty(filter);
                }
            }

            if (navigationCopy != null)
            {
                var prefabSurface = prefab.GetComponent<NavMeshSurface>();
                if (prefabSurface != null)
                {
                    prefabSurface.navMeshData = navigationCopy;
                    EditorUtility.SetDirty(prefabSurface);
                }
            }

            var keptMeshes = new HashSet<Mesh>();
            foreach (var slot in meshSlots)
            {
                if (slot.Copy != null)
                {
                    keptMeshes.Add(slot.Copy);
                }
            }

            RememberReferencedMeshes(prefab, keptMeshes);
            var prefabPath = AssetDatabase.GetAssetPath(prefab);
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(prefabPath))
            {
                if (asset == null || AssetDatabase.IsMainAsset(asset))
                {
                    continue;
                }

                if (asset is Mesh mesh)
                {
                    if (keptMeshes.Contains(mesh))
                    {
                        continue;
                    }
                }
                else if (asset is NavMeshData data)
                {
                    if (data == navigationCopy)
                    {
                        continue;
                    }
                }
                else
                {
                    continue;
                }

                AssetDatabase.RemoveObjectFromAsset(asset);
                Object.DestroyImmediate(asset, true);
            }

            RestoreGeneratedMeshes(prefab);
            EditorUtility.SetDirty(prefab);
            AssetDatabase.SaveAssets();
        }

        public static void RestoreGeneratedMeshes(GameObject prefab)
        {
            if (prefab == null)
            {
                return;
            }

            var path = AssetDatabase.GetAssetPath(prefab);
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            var meshes = new List<Mesh>();
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path))
            {
                if (asset is Mesh mesh)
                {
                    meshes.Add(mesh);
                }
            }

            var changed = false;
            foreach (var filter in prefab.GetComponentsInChildren<MeshFilter>(true))
            {
                if (filter.sharedMesh != null)
                {
                    continue;
                }

                var mesh = FindGeneratedMesh(meshes, filter.gameObject.name);
                if (mesh == null)
                {
                    continue;
                }

                filter.sharedMesh = mesh;
                EditorUtility.SetDirty(filter);
                changed = true;
            }

            foreach (var collider in prefab.GetComponentsInChildren<MeshCollider>(true))
            {
                if (collider.sharedMesh != null)
                {
                    continue;
                }

                var filter = collider.GetComponent<MeshFilter>();
                var mesh = filter != null && filter.sharedMesh != null
                    ? filter.sharedMesh
                    : FindGeneratedMesh(meshes, collider.gameObject.name);
                if (mesh == null)
                {
                    continue;
                }

                collider.sharedMesh = mesh;
                EditorUtility.SetDirty(collider);
                changed = true;
            }

            if (!changed)
            {
                return;
            }

            EditorUtility.SetDirty(prefab);
            AssetDatabase.SaveAssets();
        }

        private static void RememberReferencedMeshes(GameObject prefab, HashSet<Mesh> meshes)
        {
            foreach (var filter in prefab.GetComponentsInChildren<MeshFilter>(true))
            {
                if (filter.sharedMesh != null)
                {
                    meshes.Add(filter.sharedMesh);
                }
            }

            foreach (var collider in prefab.GetComponentsInChildren<MeshCollider>(true))
            {
                if (collider.sharedMesh != null)
                {
                    meshes.Add(collider.sharedMesh);
                }
            }
        }

        private static Mesh FindGeneratedMesh(List<Mesh> meshes, string objectName)
        {
            var suffix = objectName switch
            {
                "VisualMap" => "_Visual",
                "DungeonFloor" => "_DungeonFloor",
                "DungeonWallMesh" => "_DungeonWalls",
                _ => null
            };
            if (suffix == null)
            {
                return null;
            }

            Mesh found = null;
            foreach (var mesh in meshes)
            {
                if (mesh == null || !mesh.name.EndsWith(suffix))
                {
                    continue;
                }

                if (found != null)
                {
                    return null;
                }

                found = mesh;
            }

            return found;
        }

        private static NavMeshData CloneNavigation(GameObject root)
        {
            var surface = root.GetComponent<NavMeshSurface>();
            if (surface == null || surface.navMeshData == null || IsReusableAsset(surface.navMeshData))
            {
                return null;
            }

            var copy = Object.Instantiate(surface.navMeshData);
            copy.name = "NavMesh";
            copy.hideFlags = HideFlags.None;
            return copy;
        }

        private static Dictionary<string, Transform> IndexTransforms(Transform root)
        {
            var indexed = new Dictionary<string, Transform>();
            foreach (var transform in root.GetComponentsInChildren<Transform>(true))
            {
                indexed[IndexedPath(root, transform)] = transform;
            }

            return indexed;
        }

        private static string IndexedPath(Transform root, Transform transform)
        {
            if (transform == root)
            {
                return string.Empty;
            }

            var parts = new List<string>();
            var current = transform;
            while (current != null && current != root)
            {
                parts.Add($"{current.GetSiblingIndex()}:{current.name}");
                current = current.parent;
            }

            parts.Reverse();
            return string.Join("/", parts);
        }

        private static bool IsReusableAsset(UnityEngine.Object asset)
        {
            var path = AssetDatabase.GetAssetPath(asset);
            if (string.IsNullOrEmpty(path))
            {
                return false;
            }

            return !path.EndsWith(".unity", global::System.StringComparison.OrdinalIgnoreCase);
        }

        private static ActiveMapManager FindOrCreateManager()
        {
            var manager = ActiveMapManager.Find();
            if (manager != null)
            {
                return manager;
            }

            var managerObject = new GameObject("Active Map");
            Undo.RegisterCreatedObjectUndo(managerObject, "Create Active Map");
            return managerObject.AddComponent<ActiveMapManager>();
        }

        private static GameObject FindSceneInstance(GameObject prefab)
        {
            var maps = Object.FindObjectsByType<MapInstance>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            foreach (var map in maps)
            {
                if (map == null || !map.gameObject.scene.IsValid())
                {
                    continue;
                }

                if (PrefabUtility.GetCorrespondingObjectFromSource(map.gameObject) == prefab)
                {
                    return map.gameObject;
                }
            }

            return null;
        }

        private static void RemoveOtherMapInstances(GameObject keep)
        {
            var maps = Object.FindObjectsByType<MapInstance>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            foreach (var map in maps)
            {
                if (map == null ||
                    map.gameObject == keep ||
                    !map.gameObject.scene.IsValid())
                {
                    continue;
                }

                Undo.DestroyObjectImmediate(map.gameObject);
            }
        }
    }

    [CustomEditor(typeof(ActiveMapManager))]
    sealed class ActiveMapManagerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var prefabProperty = serializedObject.FindProperty("activeMapPrefab");
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(
                prefabProperty,
                new GUIContent(
                    "Map Prefab",
                    "Baked map. Changing this replaces the one map in the scene."));
            var changed = EditorGUI.EndChangeCheck();
            var selected = prefabProperty.objectReferenceValue as GameObject;
            if (!changed)
            {
                serializedObject.ApplyModifiedProperties();
            }
            else
            {
                serializedObject.Update();
                var manager = (ActiveMapManager)target;
                if (selected == null)
                {
                    var cleared = new SerializedObject(manager);
                    cleared.FindProperty("activeMapPrefab").objectReferenceValue = null;
                    cleared.ApplyModifiedProperties();
                }
                else if (Application.isPlaying)
                {
                    manager.ActivateBakedMap(selected);
                }
                else
                {
                    MapBakeOutput.PlacePrefabInScene(selected);
                }

                serializedObject.Update();
            }

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.ObjectField(
                    "Scene Instance",
                    ((ActiveMapManager)target).ActiveMap,
                    typeof(MapInstance),
                    true);
            }

            DrawPlayableMaps((ActiveMapManager)target);
        }

        // Placing a map dirties the inspector and rebuilds this editor. These stay on the type so the popups keep the chosen entry.
        private static int readyMapIndex;
        private static MapGameMode selectedMode;

        private void DrawPlayableMaps(ActiveMapManager manager)
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Ready Maps", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(
                serializedObject.FindProperty("stage"),
                new GUIContent(
                    "Simulation Stage",
                    "Stage that owns the slots. Slot chooses which one receives the map."));
            EditorGUILayout.PropertyField(
                serializedObject.FindProperty("playableMaps"),
                new GUIContent(
                    "Playable Catalog",
                    "The only playable-map list. Mode select and the simulation stage read this."));
            serializedObject.ApplyModifiedProperties();

            var catalog = manager.PlayableMaps;
            if (catalog == null || catalog.maps == null)
            {
                EditorGUILayout.HelpBox(
                    "Assign the playable map catalog on Active Map.",
                    MessageType.Info);
                return;
            }

            using (new EditorGUI.DisabledScope(EditorApplication.isPlaying))
            {
                if (GUILayout.Button(
                        new GUIContent(
                            "Regenerate Catalog Map Roots",
                            "Rebuilds the baked prefab for every map in this playable catalog. The scene's active map stays where it is.")))
                {
                    PlayableMapRootRegenerator.Regenerate(catalog);
                }
            }

            var ready = new List<MapDefinition>();
            foreach (var definition in catalog.maps)
            {
                if (definition != null && definition.bakedPrefab != null)
                {
                    ready.Add(definition);
                }
            }

            if (ready.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    "No ready map has a baked prefab yet. Add a map after you have played it.",
                    MessageType.Info);
            }
            else
            {
                readyMapIndex = Mathf.Clamp(readyMapIndex, 0, ready.Count - 1);
                var names = new string[ready.Count];
                for (var i = 0; i < ready.Count; i++)
                {
                    names[i] = ready[i].name;
                }

                readyMapIndex = EditorGUILayout.Popup("Ready Map", readyMapIndex, names);
            }

            var selectedMap = ready.Count == 0 ? null : ready[readyMapIndex];
            var modes = ModesFor(selectedMap);
            if (modes.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    "This map has no supported mode with one configuration.",
                    MessageType.Info);
            }
            else
            {
                var modeNames = new string[modes.Count];
                var modeIndex = 0;
                for (var i = 0; i < modes.Count; i++)
                {
                    modeNames[i] = ObjectNames.NicifyVariableName(modes[i].ToString());
                    if (modes[i] == selectedMode)
                    {
                        modeIndex = i;
                    }
                }

                modeIndex = EditorGUILayout.Popup("Game Mode", modeIndex, modeNames);
                selectedMode = modes[modeIndex];
            }

            var slotProperty = serializedObject.FindProperty("slotIndex");
            DrawSlots(manager, slotProperty.intValue);
            EditorGUILayout.IntSlider(
                slotProperty,
                0,
                SimulationStageLayout.SlotCount - 1,
                new GUIContent("Slot", "Index of the slot that receives the map."));
            serializedObject.ApplyModifiedProperties();
            if (modes.Count > 0)
            {
                var mode = selectedMode;
                if (ready.Count > 0 && GUILayout.Button("Assign To Slot"))
                {
                    AssignReadyMap(ready[readyMapIndex], mode);
                }

                if (GUILayout.Button("Assign Catalog Map For Mode"))
                {
                    var next = catalog.FirstPlayable(mode);
                    if (next == null)
                    {
                        Debug.LogError($"The playable catalog has no baked map for {mode}.");
                    }
                    else
                    {
                        AssignReadyMap(next, mode);
                    }
                }
            }

            if (GUILayout.Button("Empty Slot"))
            {
                EmptySelectedSlot();
            }

            using (new EditorGUI.DisabledScope(!Application.isPlaying))
            {
                if (GUILayout.Button("Play Slot"))
                {
                    manager.PlaySlot();
                }
            }
        }

        private static List<MapGameMode> ModesFor(MapDefinition definition)
        {
            var available = new List<MapGameMode>();
            var modes = (MapGameMode[])global::System.Enum.GetValues(typeof(MapGameMode));
            foreach (var mode in modes)
            {
                if (definition == null ||
                    (definition.SupportsMode(mode) &&
                     definition.TryGetSingleConfiguration(mode, out _)))
                {
                    available.Add(mode);
                }
            }

            return available;
        }

        private static void DrawSlots(ActiveMapManager manager, int selectedSlot)
        {
            var stageProperty = new SerializedObject(manager).FindProperty("stage");
            var stage = stageProperty == null ? null : stageProperty.objectReferenceValue as BattleRoyale.Maps.SimulationStage;
            if (stage == null)
            {
                EditorGUILayout.HelpBox(
                    "Assign a simulation stage. Each slot keeps its own map and mode.",
                    MessageType.Warning);
                return;
            }

            var stageObject = new SerializedObject(stage);
            var slots = stageObject.FindProperty("slots");
            if (slots == null)
            {
                return;
            }

            for (var index = 0; index < slots.arraySize && index < SimulationStageLayout.SlotCount; index++)
            {
                var match = slots.GetArrayElementAtIndex(index)
                    .FindPropertyRelative("match").objectReferenceValue as SimulationMatch;
                var configuration = match == null ? null : match.ModeConfiguration;
                var occupied = match != null &&
                    match.gameObject.activeInHierarchy &&
                    configuration != null;
                var label = "Empty";
                if (occupied)
                {
                    var mapName = configuration.map == null ? "none" : configuration.map.name;
                    var modeName = ObjectNames.NicifyVariableName(configuration.mode.ToString());
                    label = $"{modeName} — {mapName}";
                }

                if (index == selectedSlot)
                {
                    label += "  (selected)";
                }

                EditorGUILayout.LabelField($"Slot {index}", label);
            }
        }

        private void EmptySelectedSlot()
        {
            var manager = (ActiveMapManager)target;
            var stageObject = new SerializedObject(manager);
            var stage = stageObject.FindProperty("stage").objectReferenceValue as SimulationStage;
            var slot = stageObject.FindProperty("slotIndex").intValue;
            if (stage != null)
            {
                var slots = new SerializedObject(stage).FindProperty("slots");
                if (slots != null && slot >= 0 && slot < slots.arraySize)
                {
                    var match = slots.GetArrayElementAtIndex(slot)
                        .FindPropertyRelative("match").objectReferenceValue as SimulationMatch;
                    if (match != null)
                    {
                        Undo.RecordObject(match.gameObject, "Empty Map Slot");
                        Undo.RecordObject(match, "Empty Map Slot");
                    }
                }
            }

            Undo.RecordObject(manager, "Empty Map Slot");
            manager.ClearSlot();
            if (stage != null)
            {
                EditorSceneManager.MarkSceneDirty(stage.gameObject.scene);
            }

            EditorUtility.SetDirty(manager);
            serializedObject.Update();
        }

        private void AssignReadyMap(MapDefinition definition, MapGameMode mode)
        {
            var manager = (ActiveMapManager)target;
            var stageObject = new SerializedObject(manager);
            var stage = stageObject.FindProperty("stage").objectReferenceValue as BattleRoyale.Maps.SimulationStage;
            if (stage != null)
            {
                var slots = new SerializedObject(stage).FindProperty("slots");
                for (var index = 0; slots != null && index < slots.arraySize; index++)
                {
                    var match = slots.GetArrayElementAtIndex(index)
                        .FindPropertyRelative("match").objectReferenceValue;
                    if (match != null)
                    {
                        Undo.RecordObject(match, "Assign Map Slot");
                    }
                }
            }

            Undo.RecordObject(manager, "Assign Map Slot");
            var slot = stageObject.FindProperty("slotIndex").intValue;
            if (stage != null && !MapBakeOutput.UseModeOnSlot(stage, slot, mode))
            {
                return;
            }

            manager.AssignSlot(definition, mode);
            if (stage != null)
            {
                var slots = new SerializedObject(stage).FindProperty("slots");
                for (var index = 0; slots != null && index < slots.arraySize; index++)
                {
                    var match = slots.GetArrayElementAtIndex(index)
                        .FindPropertyRelative("match").objectReferenceValue;
                    if (match != null)
                    {
                        EditorUtility.SetDirty(match);
                    }
                }

                EditorSceneManager.MarkSceneDirty(stage.gameObject.scene);
            }

            EditorUtility.SetDirty(manager);
            serializedObject.Update();
        }
    }

    [CustomEditor(typeof(PlayableMapCatalog))]
    sealed class PlayableMapCatalogEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            EditorGUILayout.HelpBox(
                "Add a map only after you have played it. Switching uses the baked prefab on that map definition. Earlier maps are chosen first when a mode needs a replacement.",
                MessageType.Info);
            serializedObject.Update();
            EditorGUILayout.PropertyField(
                serializedObject.FindProperty("maps"),
                new GUIContent("Ready Maps"),
                true);
            serializedObject.ApplyModifiedProperties();

            var catalog = (PlayableMapCatalog)target;
            if (catalog.maps == null)
            {
                return;
            }

            var seen = new HashSet<MapDefinition>();
            foreach (var definition in catalog.maps)
            {
                if (definition == null)
                {
                    continue;
                }

                if (!seen.Add(definition))
                {
                    EditorGUILayout.HelpBox(
                        $"{definition.name} is listed more than once. The first entry is used.",
                        MessageType.Warning);
                    continue;
                }

                if (definition.bakedPrefab == null)
                {
                    EditorGUILayout.HelpBox(
                        $"{definition.name} has no baked prefab, so it cannot be placed.",
                        MessageType.Warning);
                }
            }
        }
    }
}
