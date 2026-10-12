using System.Collections.Generic;
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
        private static int modeIndex;

        private void DrawPlayableMaps(ActiveMapManager manager)
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Ready Maps", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(
                serializedObject.FindProperty("playableMaps"),
                new GUIContent(
                    "Playable Catalog",
                    "Ready maps. Empty uses the PlayableMaps asset in Resources."));
            serializedObject.ApplyModifiedProperties();

            var catalog = manager.PlayableMaps;
            if (catalog == null || catalog.maps == null)
            {
                EditorGUILayout.HelpBox(
                    "Add a Playable Map Catalog named PlayableMaps under Resources.",
                    MessageType.Info);
                return;
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
                if (GUILayout.Button("Use Ready Map"))
                {
                    var definition = ready[readyMapIndex];
                    modeIndex = ReadyModeIndex(definition, modeIndex);
                    PlaceReadyMap(definition.bakedPrefab);
                }
            }

            var modes = (MapGameMode[])global::System.Enum.GetValues(typeof(MapGameMode));
            modeIndex = Mathf.Clamp(modeIndex, 0, modes.Length - 1);
            var modeNames = new string[modes.Length];
            for (var i = 0; i < modes.Length; i++)
            {
                modeNames[i] = ObjectNames.NicifyVariableName(modes[i].ToString());
            }

            modeIndex = EditorGUILayout.Popup("Game Mode", modeIndex, modeNames);
            var mode = modes[modeIndex];
            var current = manager.ActiveMap != null ? manager.ActiveMap.definition : null;
            if (catalog.IsPlayable(current, mode))
            {
                EditorGUILayout.HelpBox(
                    "The active map is already a ready map for this mode.",
                    MessageType.None);
            }

            if (GUILayout.Button("Use Ready Map For Mode") &&
                !catalog.IsPlayable(current, mode))
            {
                var next = catalog.FirstPlayable(mode);
                if (next == null)
                {
                    Debug.LogError($"The playable catalog has no baked map for {mode}.");
                }
                else
                {
                    PlaceReadyMap(next.bakedPrefab);
                }
            }
        }

        private static int ReadyModeIndex(MapDefinition definition, int currentIndex)
        {
            var modes = (MapGameMode[])global::System.Enum.GetValues(typeof(MapGameMode));
            currentIndex = Mathf.Clamp(currentIndex, 0, modes.Length - 1);
            if (definition?.supportedGameModes == null || definition.supportedGameModes.Count == 0)
            {
                return currentIndex;
            }

            if (definition.SupportsMode(modes[currentIndex]))
            {
                return currentIndex;
            }

            var readyMode = definition.supportedGameModes[0];
            for (var i = 0; i < modes.Length; i++)
            {
                if (modes[i] == readyMode)
                {
                    return i;
                }
            }

            return currentIndex;
        }

        private void PlaceReadyMap(GameObject prefab)
        {
            var manager = (ActiveMapManager)target;
            if (Application.isPlaying)
            {
                manager.ActivateBakedMap(prefab);
            }
            else
            {
                MapBakeOutput.PlacePrefabInScene(prefab);
            }

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
