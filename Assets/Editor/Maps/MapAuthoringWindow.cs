#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BattleRoyale.Maps;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace BattleRoyale.Maps.EditorTools
{
    public sealed partial class MapAuthoringWindow : EditorWindow
    {
        private const string VisualMaterialTemplatePath = "Assets/Materials/MapPlane.mat";

        [SerializeField] private TextAsset layoutJson;
        [SerializeField] private Texture2D visualMap;
        [SerializeField] private MapDefinition mapDefinition;
        [SerializeField] private MapModeAuthoringPanel modeAuthoring = new();
        [SerializeField] private float worldScale = 1f;
        [SerializeField] private float baseHeight;
        [SerializeField] private float wallHeight = 3f;
        [SerializeField] private float wallThickness = 0.7f;
        [SerializeField] private float unitScale = DungeonMapGeometry.DefaultUnitScale;
        [SerializeField] private bool unitScaleDefaultIsOne;
        [SerializeField] private bool generateBuildingWalls = true;
        [SerializeField] private bool generateTreeColliders;
        [SerializeField] private bool replaceGeneratedRoot = true;
        [SerializeField] private bool useImageCornerFit;
        [SerializeField] private Vector2 imageUpperLeftUv = new(0f, 1f);
        [SerializeField] private Vector2 imageLowerRightUv = new(1f, 0f);
        [SerializeField] private bool editImageCornersInScene;
        [SerializeField] private Vector2 treeUpperLeft;
        [SerializeField] private Vector2 treeLowerRight;
        [SerializeField] private bool editTreeCornersInScene;
        [SerializeField] private float treeRadiusMultiplier = 1f;
        [SerializeField] private float treeHeight = 2f;

        private LayoutData analyzedLayout;
        private string statusMessage;
        private MessageType statusType = MessageType.Info;
        private Vector2 scrollPosition;
        private bool saveOptionsRequested;
        private bool createDefinitionRequested;
        private bool generateRequested;
        private bool applyUnitScaleRequested;
        private bool imageAlignmentSaveRequested;
        private bool imageAlignmentApplyRequested;
        private bool treeSaveRequested;
        private bool treeApplyRequested;
        private bool deferredActionQueued;
        private bool cornerSceneGuiRegistered;
        private bool draggingUpperLeft;
        private bool draggingLowerRight;
        private Vector3 draggedUpperLeft;
        private Vector3 draggedLowerRight;
        private bool treeSceneGuiRegistered;
        private bool draggingTreeUpperLeft;
        private bool draggingTreeLowerRight;
        private Vector3 draggedTreeUpperLeft;
        private Vector3 draggedTreeLowerRight;

        [MenuItem("Tools/Maps/Village Map Authoring")]
        public static void Open()
        {
            Open(null);
        }

        private void OnEnable()
        {
            if (unitScaleDefaultIsOne || mapDefinition != null)
            {
                unitScaleDefaultIsOne = true;
                return;
            }

            unitScale = DungeonMapGeometry.DefaultUnitScale;
            unitScaleDefaultIsOne = true;
        }

        public static void Open(MapDefinition definition)
        {
            var window = GetWindow<MapAuthoringWindow>();
            window.titleContent = new GUIContent("Village Map Authoring");
            window.minSize = new Vector2(420f, 600f);
            if (definition == null)
            {
                return;
            }

            window.mapDefinition = definition;
            window.LoadDefinitionIntoWindow();
            window.Repaint();
        }

        private void OnDisable()
        {
            EditorApplication.delayCall -= RunDeferredActions;
            deferredActionQueued = false;
            StopCornerSceneGui();
            StopTreeSceneGui();
            modeAuthoring?.Dispose();
        }

        private void OnGUI()
        {
            AuthoringSectionHeader.DrawToolbar("Village");
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            AuthoringSectionHeader.Begin("Village");
            try
            {
                EditorGUILayout.HelpBox(
                    "Import a handpicked map export and its PNG. Create a map definition, then generate a flat visual floor and building collision.",
                    MessageType.Info);

                DrawSourceSection();
                DrawAnalysisSection();
                DrawAssetSection();
                DrawGeometrySection();
                DrawSettingsSection();
                DrawGenerationSection();
                DrawImageCornerSection();
                modeAuthoring ??= new MapModeAuthoringPanel();
                modeAuthoring.Draw(
                    mapDefinition,
                    MapModeAuthoringPanel.GetDefinitionsFolder(
                        layoutJson != null
                            ? layoutJson
                            : mapDefinition != null
                                ? mapDefinition.layoutJson
                                : null));
                AuthoringSectionHeader.End();
                EditorGUILayout.EndScrollView();
            }
            catch (Exception)
            {
                AuthoringSectionHeader.End();
                throw;
            }
        }

        private void QueueDeferredAction()
        {
            if (deferredActionQueued)
            {
                return;
            }

            deferredActionQueued = true;
            EditorApplication.delayCall += RunDeferredActions;
        }

        private void RunDeferredActions()
        {
            deferredActionQueued = false;
            if (this == null)
            {
                return;
            }

            if (applyUnitScaleRequested)
            {
                applyUnitScaleRequested = false;
                var scaledUnitCount = MapGeometrySection.ApplyToLoadedUnits(unitScale);
                SetStatus(
                    $"Applied unit scale {unitScale:0.##} to {scaledUnitCount} unit(s).",
                    MessageType.Info);
            }

            if (saveOptionsRequested)
            {
                saveOptionsRequested = false;
                CopyGenerationOptionsToDefinition();
            }

            if (createDefinitionRequested)
            {
                createDefinitionRequested = false;
                CreateOrUpdateDefinition();
            }

            if (imageAlignmentSaveRequested)
            {
                imageAlignmentSaveRequested = false;
                CopyImageCornersToDefinition();
            }

            if (imageAlignmentApplyRequested)
            {
                imageAlignmentApplyRequested = false;
                ApplyImageCornerFitToScene();
            }

            if (treeSaveRequested)
            {
                treeSaveRequested = false;
                CopyTreeCornersToDefinition();
            }

            if (treeApplyRequested)
            {
                treeApplyRequested = false;
                ApplyTreePointsToScene();
            }

            if (generateRequested)
            {
                generateRequested = false;
                GenerateMapRoot();
            }

            Repaint();
        }

        private void DrawSourceSection()
        {
            if (!AuthoringSectionHeader.Draw("Import"))
            {
                return;
            }


            var previousJson = layoutJson;
            layoutJson = (TextAsset)EditorGUILayout.ObjectField(
                "Layout JSON",
                layoutJson,
                typeof(TextAsset),
                false);

            visualMap = (Texture2D)EditorGUILayout.ObjectField(
                "Visual PNG (optional)",
                visualMap,
                typeof(Texture2D),
                false);

            if (layoutJson != previousJson)
            {
                analyzedLayout = null;
                if (visualMap == null)
                {
                    visualMap = FindSiblingVisualMap(layoutJson);
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Analyze JSON"))
                {
                    AnalyzeLayout();
                }

                if (GUILayout.Button("Use Selected Asset"))
                {
                    LoadDefinitionIntoWindow();
                }
            }
        }

        private void DrawGeometrySection()
        {
            var values = new MapGeometrySection.Values
            {
                WorldScale = worldScale,
                BaseHeight = baseHeight,
                WallHeight = wallHeight,
                WallThickness = wallThickness,
                UnitScale = unitScale
            };
            MapGeometrySection.Draw(ref values, out var changed, out var applyRequested);
            worldScale = values.WorldScale;
            baseHeight = values.BaseHeight;
            wallHeight = values.WallHeight;
            wallThickness = values.WallThickness;
            unitScale = values.UnitScale;
            if (changed)
            {
                saveOptionsRequested = true;
                QueueDeferredAction();
            }

            if (applyRequested)
            {
                applyUnitScaleRequested = true;
                QueueDeferredAction();
            }
        }

        private void DrawSettingsSection()
        {
            if (!AuthoringSectionHeader.Draw("Map Settings"))
            {
                StopTreeSceneGui();
                return;
            }

            var previousTrees = generateTreeColliders;
            EditorGUI.BeginChangeCheck();
            generateBuildingWalls = EditorGUILayout.Toggle(
                "Generate Building Walls",
                generateBuildingWalls);
            generateTreeColliders = EditorGUILayout.Toggle(
                new GUIContent(
                    "Generate Trees",
                    "Builds one tree mesh and collider. Radius and height set the cylinder. Corner points move the whole tree area."),
                generateTreeColliders);
            replaceGeneratedRoot = EditorGUILayout.Toggle(
                new GUIContent(
                    "Replace Existing Map Prefab",
                    "Generating saves a baked prefab beside the map definition and places it as the one map in the scene."),
                replaceGeneratedRoot);
            if (EditorGUI.EndChangeCheck())
            {
                saveOptionsRequested = true;
                QueueDeferredAction();
            }

            if (!generateTreeColliders)
            {
                StopTreeSceneGui();
                if (previousTrees)
                {
                    RemoveTreeCornerMarkers();
                    treeSaveRequested = true;
                    QueueDeferredAction();
                }

                return;
            }

            if (!previousTrees)
            {
                EnsureDefaultTreeCorners();
            }

            DrawTreeCornerOptions();
        }

        private void DrawAnalysisSection()
        {
            if (!AuthoringSectionHeader.Draw("Analysis"))
            {
                return;
            }


            if (analyzedLayout == null)
            {
                EditorGUILayout.HelpBox(
                    "Analyze the JSON to calculate bounds and inspect exported feature groups.",
                    MessageType.None);
                return;
            }

            EditorGUILayout.LabelField(
                "Generator",
                $"{analyzedLayout.generator} {analyzedLayout.generatorVersion}".Trim());
            EditorGUILayout.LabelField(
                "Source Min",
                analyzedLayout.sourceMin.ToString("F3"));
            EditorGUILayout.LabelField(
                "Source Max",
                analyzedLayout.sourceMax.ToString("F3"));
            EditorGUILayout.LabelField(
                "Source Size",
                (analyzedLayout.sourceMax - analyzedLayout.sourceMin).ToString("F3"));
            EditorGUILayout.LabelField(
                "Building Rings",
                analyzedLayout.buildingRings.Count.ToString());
            EditorGUILayout.LabelField(
                "Tree Points",
                analyzedLayout.treePoints.Count.ToString());

            foreach (var feature in analyzedLayout.summaries)
            {
                EditorGUILayout.LabelField(
                    feature.id,
                    $"{feature.type} | geometries: {feature.geometryCount} | coordinates: {feature.coordinateCount}");
            }
        }

        private void DrawAssetSection()
        {
            if (!AuthoringSectionHeader.Draw("Map Definition"))
            {
                return;
            }

            mapDefinition = (MapDefinition)EditorGUILayout.ObjectField(
                "Map Asset",
                mapDefinition,
                typeof(MapDefinition),
                false);

            if (GUILayout.Button("Create / Update Map Definition"))
            {
                createDefinitionRequested = true;
                QueueDeferredAction();
            }

            if (IsMapReady())
            {
                EditorGUILayout.ObjectField(
                    "Assigned JSON",
                    mapDefinition.layoutJson,
                    typeof(TextAsset),
                    false);
                EditorGUILayout.ObjectField(
                    "Narrative Markdown",
                    mapDefinition.narrativeMarkdown,
                    typeof(TextAsset),
                    false);
                EditorGUILayout.ObjectField(
                    "Assigned Visual",
                    mapDefinition.visualMap,
                    typeof(Texture2D),
                    false);
            }
        }

        private void DrawGenerationSection()
        {
            if (!AuthoringSectionHeader.Draw("Generate"))
            {
                return;
            }


            using (new EditorGUI.DisabledScope(!IsMapReady()))
            {
                if (GUILayout.Button("Generate Map Root"))
                {
                    generateRequested = true;
                    QueueDeferredAction();
                }
            }

            if (!string.IsNullOrWhiteSpace(statusMessage))
            {
                EditorGUILayout.HelpBox(statusMessage, statusType);
            }
        }

        private void AnalyzeLayout()
        {
            if (layoutJson == null)
            {
                SetStatus("Assign a JSON layout before analyzing.", MessageType.Error);
                return;
            }

            try
            {
                analyzedLayout = LayoutReader.Read(layoutJson.text);
                if (visualMap == null)
                {
                    visualMap = FindSiblingVisualMap(layoutJson);
                }

                SetStatus(
                    $"Analyzed {layoutJson.name}: {analyzedLayout.summaries.Count} feature groups found.",
                    MessageType.Info);
            }
            catch (Exception exception)
            {
                analyzedLayout = null;
                Debug.LogException(exception);
                SetStatus($"Could not parse {layoutJson.name}: {exception.Message}", MessageType.Error);
            }
        }

        private void CreateOrUpdateDefinition()
        {
            if (analyzedLayout == null)
            {
                AnalyzeLayout();
            }

            if (analyzedLayout == null || layoutJson == null)
            {
                return;
            }

            var definitionsFolder = MapModeAuthoringPanel.EnsureDefinitionsFolder(layoutJson);
            if (string.IsNullOrEmpty(definitionsFolder) ||
                !AssetDatabase.IsValidFolder(definitionsFolder))
            {
                SetStatus(
                    "Save the layout JSON in the project before creating its Definitions folder.",
                    MessageType.Error);
                return;
            }

            if (mapDefinition == null)
            {
                var mapName = Path.GetFileNameWithoutExtension(layoutJson.name);
                var assetPath = $"{definitionsFolder}/{mapName}_Map.asset";
                mapDefinition = AssetDatabase.LoadAssetAtPath<MapDefinition>(assetPath);
                if (mapDefinition == null)
                {
                    assetPath = AssetDatabase.GenerateUniqueAssetPath(assetPath);
                    mapDefinition = CreateInstance<MapDefinition>();
                    mapDefinition.name = $"{mapName}_Map";
                    AssetDatabase.CreateAsset(mapDefinition, assetPath);
                }
            }

            mapDefinition.layoutJson = layoutJson;
            var narrativeMarkdown = MapModeAuthoringPanel.FindSiblingMarkdown(layoutJson);
            if (narrativeMarkdown != null)
            {
                mapDefinition.narrativeMarkdown = narrativeMarkdown;
            }
            mapDefinition.visualMap = visualMap != null
                ? visualMap
                : FindSiblingVisualMap(layoutJson);
            mapDefinition.visualSprite = MapModeAuthoringPanel.SpriteForVisualMap(
                mapDefinition.visualMap);
            mapDefinition.worldScale = worldScale;
            mapDefinition.baseHeight = baseHeight;
            mapDefinition.wallHeight = wallHeight;
            mapDefinition.wallThickness = wallThickness;
            mapDefinition.unitScale = unitScale;
            mapDefinition.generateBuildingWalls = generateBuildingWalls;
            mapDefinition.generateTreeColliders = generateTreeColliders;
            mapDefinition.replaceGeneratedRoot = replaceGeneratedRoot;
            mapDefinition.useImageCornerFit = useImageCornerFit;
            mapDefinition.imageUpperLeftUv = imageUpperLeftUv;
            mapDefinition.imageLowerRightUv = imageLowerRightUv;
            mapDefinition.editImageCornersInScene = editImageCornersInScene;
            mapDefinition.treeUpperLeft = treeUpperLeft;
            mapDefinition.treeLowerRight = treeLowerRight;
            mapDefinition.editTreeCornersInScene = editTreeCornersInScene;
            mapDefinition.treeRadiusMultiplier = TreeRadiusScale();
            mapDefinition.treeHeight = TreeHeight();
            mapDefinition.sourceMin = analyzedLayout.sourceMin;
            mapDefinition.sourceMax = analyzedLayout.sourceMax;
            mapDefinition.generator = analyzedLayout.generator;
            mapDefinition.generatorVersion = analyzedLayout.generatorVersion;
            mapDefinition.features = analyzedLayout.summaries.ToArray();

            EditorUtility.SetDirty(mapDefinition);
            AssetDatabase.SaveAssets();
            Selection.activeObject = mapDefinition;

            SetStatus(
                $"Map definition saved at {AssetDatabase.GetAssetPath(mapDefinition)}.",
                MessageType.Info);
        }

        private void GenerateMapRoot()
        {
            if (EditorApplication.isPlaying)
            {
                SetStatus("Stop Play Mode before generating a map prefab.", MessageType.Error);
                return;
            }

            if (!IsMapReady())
            {
                SetStatus("Create a map definition before generating a map prefab.", MessageType.Error);
                return;
            }

            if (analyzedLayout == null ||
                analyzedLayout.sourceMin != mapDefinition.sourceMin ||
                analyzedLayout.sourceMax != mapDefinition.sourceMax)
            {
                layoutJson = mapDefinition.layoutJson;
                AnalyzeLayout();
            }

            if (analyzedLayout == null)
            {
                return;
            }

            if (!modeAuthoring.ValidateForGeneration(
                    mapDefinition,
                    out var modeValidationError))
            {
                SetStatus(modeValidationError, MessageType.Error);
                return;
            }

            var prefabPath = MapBakeOutput.GetPrefabPath(mapDefinition);
            if (!replaceGeneratedRoot &&
                !string.IsNullOrEmpty(prefabPath) &&
                AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null)
            {
                SetStatus(
                    $"A map prefab already exists at {prefabPath}.",
                    MessageType.Warning);
                Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                return;
            }

            var root = new GameObject("Map");
            Undo.RegisterCreatedObjectUndo(root, "Generate Map Prefab");
            var mapInstance = root.AddComponent<MapInstance>();
            mapInstance.definition = mapDefinition;

            CopyImageCornersToDefinition();
            CopyTreeCornersToDefinition();
            CreateVisualMap(root.transform);
            if (useImageCornerFit)
            {
                SyncImageCornerMarkers(root.transform);
            }

            CreateFloorCollider(root.transform);
            CreateBoundaryWalls(root.transform);
            CreateWater(root.transform);
            CreateBuildingWalls(root.transform);

            if (generateTreeColliders)
            {
                CreateTreeColliders(root.transform);
            }

            modeAuthoring.GenerateMarkers(root.transform, mapDefinition);
            var placed = MapBakeOutput.Commit(root, mapDefinition, true);
            SetStatus(
                placed != null
                    ? $"Saved {prefabPath} and placed it as the active map."
                    : "The map was built, but its prefab was not placed as the active map.",
                placed != null ? MessageType.Info : MessageType.Error);
        }

        private void CreateVisualMap(Transform parent)
        {
            var visualObject = new GameObject("VisualMap");
            visualObject.transform.SetParent(parent, false);
            visualObject.transform.position = new Vector3(
                0f,
                mapDefinition.baseHeight - 0.02f,
                0f);

            var meshFilter = visualObject.AddComponent<MeshFilter>();
            var meshRenderer = visualObject.AddComponent<MeshRenderer>();
            meshFilter.sharedMesh = BuildVisualQuad();
            var visualMaterial = GetOrCreateVisualMaterial();
            meshRenderer.sharedMaterial = visualMaterial;
            meshRenderer.enabled = visualMaterial != null;
        }

        private void CreateFloorCollider(Transform parent)
        {
            var floorObject = new GameObject("VillageFloor");
            floorObject.transform.SetParent(parent, false);
            var groundLayer = LayerMask.NameToLayer("MapGround");
            if (groundLayer >= 0)
            {
                floorObject.layer = groundLayer;
            }

            var corners = VisualCorners();
            DungeonFloorBox.GetCenterAndSize(
                corners[0],
                corners[1],
                corners[2],
                corners[3],
                mapDefinition.baseHeight,
                DungeonFloorBox.Thickness,
                out var center,
                out var size);
            var collider = floorObject.AddComponent<BoxCollider>();
            collider.center = center;
            collider.size = size;
        }

        private void CreateBoundaryWalls(Transform parent)
        {
            if (!TryVisualRect(out var min, out var max))
            {
                return;
            }

            var height = mapDefinition.wallHeight > 0.01f ? mapDefinition.wallHeight : 2f;
            var thickness = mapDefinition.wallThickness > 0.01f ? mapDefinition.wallThickness : 1f;
            MapBoundaryWalls.Create(
                parent,
                $"{mapDefinition.name}_BoundaryWalls",
                min,
                max,
                thickness,
                height,
                mapDefinition.baseHeight);
        }

        private bool TryVisualRect(out Vector2 min, out Vector2 max)
        {
            return MapPlayBounds.TryGetRect(VisualCorners(), out min, out max);
        }

        private bool TryInnerRect(float inset, out Vector2 min, out Vector2 max)
        {
            if (!TryVisualRect(out var visualMin, out var visualMax))
            {
                min = default;
                max = default;
                return false;
            }

            MapPlayBounds.TryInset(visualMin, visualMax, inset, out min, out max);
            return true;
        }

        private void CreateWater(Transform parent)
        {
            if (analyzedLayout.waterRings.Count == 0)
            {
                return;
            }

            var bridgeIndex = 0;
            var skippedBridges = 0;
            var deckWidth = analyzedLayout.roadWidth > 0.01f ? analyzedLayout.roadWidth : 7f;
            foreach (var ring in analyzedLayout.waterRings)
            {
                var clippedRing = ClipWaterRing(ring);
                if (clippedRing.Count < 3)
                {
                    continue;
                }

                var vertices = new List<Vector3>();
                var triangles = new List<int>();
                var decks = new List<List<Vector2>>();
                var openings = VillageWaterGeometry.FindBridgeOpenings(
                    clippedRing,
                    analyzedLayout.roads,
                    deckWidth,
                    deckWidth);
                if (!VillageBridgeCut.TryBuildOpenedVolume(
                        clippedRing,
                        openings,
                        VillageWaterGeometry.BlockHeight,
                        vertices,
                        triangles,
                        decks,
                        out var skipped))
                {
                    SetStatus(
                        "The river outline could not be turned into a collider.",
                        MessageType.Warning);
                    continue;
                }

                skippedBridges += skipped;
                ScaleMapHorizontal(vertices);
                var water = CreateGeneratedMesh(
                    parent,
                    "Water",
                    vertices,
                    triangles,
                    new Color(0.2f, 0.45f, 0.85f, 1f),
                    true);
                water.transform.localPosition = new Vector3(0f, mapDefinition.baseHeight, 0f);

                for (var i = 0; i < decks.Count; i++)
                {
                    var deckVertices = new List<Vector3>();
                    var deckTriangles = new List<int>();
                    if (!VillageWaterGeometry.TryBuildDeck(
                            decks[i],
                            VillageWaterGeometry.DeckClearance,
                            deckVertices,
                            deckTriangles))
                    {
                        continue;
                    }

                    ScaleMapHorizontal(deckVertices);
                    var bridge = CreateGeneratedMesh(
                        parent,
                        $"Bridge_{bridgeIndex:000}",
                        deckVertices,
                        deckTriangles,
                        new Color(0.55f, 0.42f, 0.28f, 1f),
                        false);
                    bridge.transform.localPosition = new Vector3(0f, mapDefinition.baseHeight, 0f);
                    bridgeIndex++;
                }
            }

            if (skippedBridges > 0)
            {
                SetStatus(
                    "A road crossing could not be opened through both banks.",
                    MessageType.Warning);
            }
        }

        private void ScaleMapHorizontal(List<Vector3> vertices)
        {
            var scale = mapDefinition.worldScale;
            for (var i = 0; i < vertices.Count; i++)
            {
                var point = vertices[i];
                vertices[i] = new Vector3(point.x * scale, point.y, point.z * scale);
            }
        }

        private GameObject CreateGeneratedMesh(
            Transform parent,
            string objectName,
            List<Vector3> vertices,
            List<int> triangles,
            Color color,
            bool blocking)
        {
            var meshObject = new GameObject(objectName);
            meshObject.transform.SetParent(parent, false);
            if (blocking)
            {
                var obstacleLayer = LayerMask.NameToLayer("MapObstacle");
                if (obstacleLayer >= 0)
                {
                    meshObject.layer = obstacleLayer;
                }
            }

            var mesh = new Mesh { name = $"{mapDefinition.name}_{objectName}" };
            if (vertices.Count > 65535)
            {
                mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            }

            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            meshObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = meshObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = WallBoxMesh.SharedMaterial();
            var block = new MaterialPropertyBlock();
            block.SetColor("_BaseColor", color);
            block.SetColor("_Color", color);
            renderer.SetPropertyBlock(block);
            if (blocking)
            {
                var collider = meshObject.AddComponent<MeshCollider>();
                collider.sharedMesh = mesh;
                collider.convex = false;
            }

            return meshObject;
        }

        private List<Vector2> ClipWaterRing(List<Vector2> ring)
        {
            if (!TryVisualRect(out var worldMin, out var worldMax) ||
                mapDefinition.worldScale <= 0.0001f)
            {
                return ring;
            }

            var scale = mapDefinition.worldScale;
            return MapPlayBounds.ClipPolygon(
                ring,
                worldMin / scale,
                worldMax / scale);
        }

        private Mesh BuildVisualQuad()
        {
            var mesh = new Mesh
            {
                name = $"{mapDefinition.name}_Visual"
            };
            var corners = VisualCorners();
            mesh.vertices = corners;
            mesh.uv = new[]
            {
                new Vector2(0f, 1f),
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(1f, 1f)
            };
            // Source Y grows with world Z. This winding faces up so the top-down camera sees the PNG.
            mesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private Vector3[] SourceCorners()
        {
            var scale = mapDefinition.worldScale;
            var min = mapDefinition.sourceMin;
            var max = mapDefinition.sourceMax;
            return new[]
            {
                new Vector3(min.x * scale, 0f, max.y * scale),
                new Vector3(min.x * scale, 0f, min.y * scale),
                new Vector3(max.x * scale, 0f, min.y * scale),
                new Vector3(max.x * scale, 0f, max.y * scale)
            };
        }

        private Material GetOrCreateVisualMaterial()
        {
            if (mapDefinition.visualMap == null)
            {
                return null;
            }

            var material = mapDefinition.visualMaterial;
            if (material == null)
            {
                var mapName = mapDefinition.name.Replace("_Map", string.Empty);
                var definitionsFolder = MapModeAuthoringPanel.EnsureDefinitionsFolder(
                    mapDefinition.layoutJson);
                if (string.IsNullOrEmpty(definitionsFolder) ||
                    !AssetDatabase.IsValidFolder(definitionsFolder))
                {
                    return null;
                }

                var materialPath = $"{definitionsFolder}/{mapName}_Visual.mat";
                material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);

                if (material == null)
                {
                    var template = AssetDatabase.LoadAssetAtPath<Material>(
                        VisualMaterialTemplatePath);
                    if (template != null)
                    {
                        material = new Material(template);
                    }
                    else
                    {
                        var shader = Shader.Find("Universal Render Pipeline/Lit") ??
                                     Shader.Find("Standard");
                        if (shader == null)
                        {
                            SetStatus(
                                "No compatible material shader was found for the visual map.",
                                MessageType.Warning);
                            return null;
                        }

                        material = new Material(shader);
                    }

                    material.name = $"{mapName}_Visual";
                    materialPath = AssetDatabase.GenerateUniqueAssetPath(materialPath);
                    AssetDatabase.CreateAsset(material, materialPath);
                }
            }

            if (material.HasProperty("_BaseMap"))
            {
                material.SetTexture("_BaseMap", mapDefinition.visualMap);
            }

            if (material.HasProperty("_MainTex"))
            {
                material.SetTexture("_MainTex", mapDefinition.visualMap);
            }

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", Color.white);
            }

            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", Color.white);
            }

            ConfigureVisualMaterial(material);

            mapDefinition.visualMaterial = material;
            EditorUtility.SetDirty(material);
            EditorUtility.SetDirty(mapDefinition);
            AssetDatabase.SaveAssets();
            return material;
        }

        private static void ConfigureVisualMaterial(Material material)
        {
            if (material.HasProperty("_AlphaClip"))
            {
                material.SetFloat("_AlphaClip", 1f);
            }

            if (material.HasProperty("_AlphaToMask"))
            {
                material.SetFloat("_AlphaToMask", 1f);
            }

            material.EnableKeyword("_ALPHATEST_ON");
            material.SetOverrideTag("RenderType", "TransparentCutout");
            material.renderQueue = (int)RenderQueue.AlphaTest;

            if (material.HasProperty("_ReceiveShadows"))
            {
                material.SetFloat("_ReceiveShadows", 0f);
            }

            material.EnableKeyword("_RECEIVE_SHADOWS_OFF");
        }

        private void CreateBuildingWalls(Transform parent)
        {
            if (!mapDefinition.generateBuildingWalls ||
                mapDefinition.wallHeight <= 0f ||
                analyzedLayout == null)
            {
                return;
            }

            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            var scale = mapDefinition.worldScale;
            var halfThickness = mapDefinition.wallThickness * 0.5f;
            if (!TryInnerRect(halfThickness, out var innerMin, out var innerMax))
            {
                return;
            }

            foreach (var ring in analyzedLayout.buildingRings)
            {
                for (var i = 0; i < ring.Count; i++)
                {
                    var start = ring[i];
                    var end = ring[(i + 1) % ring.Count];
                    if (!MapPlayBounds.TryClipSegment(
                            new Vector2(start.x * scale, start.y * scale),
                            new Vector2(end.x * scale, end.y * scale),
                            innerMin,
                            innerMax,
                            out var clippedStart,
                            out var clippedEnd))
                    {
                        continue;
                    }

                    var edge = clippedEnd - clippedStart;
                    var length = edge.magnitude;
                    if (length <= 0.001f)
                    {
                        continue;
                    }

                    var midpoint = (clippedStart + clippedEnd) * 0.5f;
                    var center = new Vector3(
                        midpoint.x,
                        mapDefinition.baseHeight + mapDefinition.wallHeight * 0.5f,
                        midpoint.y);
                    WallBoxMesh.AppendBox(
                        vertices,
                        triangles,
                        center,
                        new Vector3(length, mapDefinition.wallHeight, mapDefinition.wallThickness),
                        Quaternion.Euler(0f, Mathf.Atan2(edge.y, edge.x) * Mathf.Rad2Deg, 0f));
                }
            }

            if (vertices.Count == 0)
            {
                return;
            }

            var wallObject = new GameObject("BuildingWallMesh");
            wallObject.transform.SetParent(parent, false);
            var obstacleLayer = LayerMask.NameToLayer("MapObstacle");
            if (obstacleLayer >= 0)
            {
                wallObject.layer = obstacleLayer;
            }

            var mesh = WallBoxMesh.Build($"{mapDefinition.name}_BuildingWalls", vertices, triangles);
            wallObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            var collider = wallObject.AddComponent<MeshCollider>();
            collider.sharedMesh = mesh;
            collider.convex = false;
            wallObject.AddComponent<MeshRenderer>().sharedMaterial = WallBoxMesh.SharedMaterial();
        }

        private void CreateTreeColliders(Transform parent)
        {
            var mesh = BuildTreeMesh();
            if (mesh == null)
            {
                return;
            }

            var treeRoot = new GameObject("Trees");
            treeRoot.transform.SetParent(parent, false);
            var obstacleLayer = LayerMask.NameToLayer("MapObstacle");
            if (obstacleLayer >= 0)
            {
                treeRoot.layer = obstacleLayer;
            }

            treeRoot.AddComponent<MeshFilter>().sharedMesh = mesh;
            var collider = treeRoot.AddComponent<MeshCollider>();
            collider.sharedMesh = mesh;
            collider.convex = false;
            treeRoot.AddComponent<MeshRenderer>().sharedMaterial = WallBoxMesh.SharedMaterial();
        }

        Mesh BuildTreeMesh()
        {
            if (analyzedLayout == null || mapDefinition == null)
            {
                return null;
            }

            var source = SharedTreeMesh();
            var height = TreeHeight();
            var scale = new Vector3(TreeRadiusScale(), height / 2f, TreeRadiusScale());
            var treeRadius = 0.5f * scale.x;
            var keepTreesInside = TryInnerRect(treeRadius, out var treeMin, out var treeMax);
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            for (var i = 0; i < analyzedLayout.treePoints.Count; i++)
            {
                var placed = PlacedTreePoint(analyzedLayout.treePoints[i]);
                var world = new Vector2(
                    placed.x * mapDefinition.worldScale,
                    placed.y * mapDefinition.worldScale);
                if (keepTreesInside && !MapPlayBounds.Contains(world, treeMin, treeMax))
                {
                    continue;
                }

                WallBoxMesh.AppendMesh(
                    vertices,
                    triangles,
                    source,
                    new Vector3(world.x, mapDefinition.baseHeight + height * 0.5f, world.y),
                    scale);
            }

            if (vertices.Count == 0)
            {
                return null;
            }

            return WallBoxMesh.Build($"{mapDefinition.name}_Trees", vertices, triangles);
        }

        static Mesh sharedTreeMesh;

        static Mesh SharedTreeMesh()
        {
            if (sharedTreeMesh != null)
            {
                return sharedTreeMesh;
            }

            var temp = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            sharedTreeMesh = temp.GetComponent<MeshFilter>().sharedMesh;
            DestroyImmediate(temp);
            return sharedTreeMesh;
        }

        private void CopyGenerationOptionsToDefinition()
        {
            if (mapDefinition == null)
            {
                return;
            }

            mapDefinition.generateBuildingWalls = generateBuildingWalls;
            mapDefinition.generateTreeColliders = generateTreeColliders;
            mapDefinition.replaceGeneratedRoot = replaceGeneratedRoot;
            mapDefinition.worldScale = worldScale;
            mapDefinition.baseHeight = baseHeight;
            mapDefinition.wallHeight = wallHeight;
            mapDefinition.wallThickness = wallThickness;
            mapDefinition.unitScale = unitScale;
            mapDefinition.useImageCornerFit = useImageCornerFit;
            mapDefinition.imageUpperLeftUv = imageUpperLeftUv;
            mapDefinition.imageLowerRightUv = imageLowerRightUv;
            mapDefinition.editImageCornersInScene = editImageCornersInScene;
            mapDefinition.treeUpperLeft = treeUpperLeft;
            mapDefinition.treeLowerRight = treeLowerRight;
            mapDefinition.editTreeCornersInScene = editTreeCornersInScene;
            mapDefinition.treeRadiusMultiplier = TreeRadiusScale();
            mapDefinition.treeHeight = TreeHeight();
            EditorUtility.SetDirty(mapDefinition);
            AssetDatabase.SaveAssets();
        }

        private void LoadDefinitionIntoWindow()
        {
            if (mapDefinition == null)
            {
                SetStatus("Select a MapDefinition asset first.", MessageType.Warning);
                return;
            }

            layoutJson = mapDefinition.layoutJson;
            visualMap = mapDefinition.visualMap;
            worldScale = mapDefinition.worldScale;
            baseHeight = mapDefinition.baseHeight;
            wallHeight = mapDefinition.wallHeight;
            wallThickness = mapDefinition.wallThickness;
            unitScale = mapDefinition.unitScale;
            generateBuildingWalls = mapDefinition.generateBuildingWalls;
            generateTreeColliders = mapDefinition.generateTreeColliders;
            replaceGeneratedRoot = mapDefinition.replaceGeneratedRoot;
            useImageCornerFit = mapDefinition.useImageCornerFit;
            imageUpperLeftUv = mapDefinition.imageUpperLeftUv;
            imageLowerRightUv = mapDefinition.imageLowerRightUv;
            editImageCornersInScene = mapDefinition.editImageCornersInScene;
            treeUpperLeft = mapDefinition.treeUpperLeft;
            treeLowerRight = mapDefinition.treeLowerRight;
            editTreeCornersInScene = mapDefinition.editTreeCornersInScene;
            treeRadiusMultiplier = mapDefinition.treeRadiusMultiplier > 0f
                ? mapDefinition.treeRadiusMultiplier
                : 1f;
            treeHeight = mapDefinition.treeHeight > 0f
                ? mapDefinition.treeHeight
                : 2f;
            AnalyzeLayout();
            EnsureDefaultTreeCorners();
        }

        private bool IsMapReady()
        {
            return mapDefinition != null &&
                   mapDefinition.layoutJson != null &&
                   mapDefinition.HasBounds;
        }

        private static Texture2D FindSiblingVisualMap(TextAsset jsonAsset)
        {
            if (jsonAsset == null)
            {
                return null;
            }

            var jsonPath = AssetDatabase.GetAssetPath(jsonAsset);
            var pngPath = Path.ChangeExtension(jsonPath, ".png");
            return AssetDatabase.LoadAssetAtPath<Texture2D>(pngPath);
        }

        private void SetStatus(string message, MessageType type)
        {
            statusMessage = message;
            statusType = type;
        }

        private sealed class LayoutData
        {
            public string generator;
            public string generatorVersion;
            public Vector2 sourceMin;
            public Vector2 sourceMax;
            public readonly List<MapFeatureSummary> summaries = new();
            public readonly List<List<Vector2>> buildingRings = new();
            public readonly List<Vector2> treePoints = new();
            public readonly List<List<Vector2>> waterRings = new();
            public readonly List<VillageRoadLine> roads = new();
            public float roadWidth = 7f;
        }

        private static class LayoutReader
        {
            public static LayoutData Read(string json)
            {
                var root = JObject.Parse(json);
                var data = new LayoutData();
                var allPoints = new List<Vector2>();
                var earthPoints = new List<Vector2>();
                var features = root["features"] as JArray;

                if (features == null)
                {
                    throw new InvalidDataException("The JSON does not contain a features array.");
                }

                foreach (var featureToken in features.OfType<JObject>())
                {
                    var id = featureToken.Value<string>("id") ?? "unnamed";
                    var type = featureToken.Value<string>("type") ?? "unknown";
                    var coordinates = featureToken["coordinates"];
                    var geometries = featureToken["geometries"] as JArray;
                    var featurePoints = new List<Vector2>();

                    CollectPoints(coordinates, featurePoints);
                    if (geometries != null)
                    {
                        foreach (var geometry in geometries.OfType<JObject>())
                        {
                            CollectPoints(geometry["coordinates"], featurePoints);
                        }
                    }

                    allPoints.AddRange(featurePoints);
                    var geometryCount = geometries != null
                        ? geometries.Count
                        : CountFeatureGeometries(type, coordinates);
                    data.summaries.Add(new MapFeatureSummary
                    {
                        id = id,
                        type = type,
                        geometryCount = geometryCount,
                        coordinateCount = featurePoints.Count
                    });

                    if (id == "earth")
                    {
                        earthPoints.AddRange(featurePoints);
                    }

                    if (id == "buildings")
                    {
                        data.buildingRings.AddRange(ReadOuterRings(type, coordinates));
                    }

                    if (id == "trees")
                    {
                        data.treePoints.AddRange(ReadPointFeatures(coordinates));
                    }

                    if (id == "water")
                    {
                        data.waterRings.AddRange(ReadOuterRings(type, coordinates));
                    }

                    if (id == "roads")
                    {
                        ReadRoads(geometries, data.roads);
                    }

                    if (id == "values")
                    {
                        data.generator = featureToken.Value<string>("generator") ?? string.Empty;
                        data.generatorVersion = featureToken.Value<string>("version") ?? string.Empty;
                        data.roadWidth = featureToken.Value<float?>("roadWidth") ?? data.roadWidth;
                    }
                }

                var boundsPoints = earthPoints.Count >= 2 ? earthPoints : allPoints;
                if (boundsPoints.Count == 0)
                {
                    throw new InvalidDataException("The JSON does not contain coordinate data.");
                }

                data.sourceMin = new Vector2(
                    boundsPoints.Min(point => point.x),
                    boundsPoints.Min(point => point.y));
                data.sourceMax = new Vector2(
                    boundsPoints.Max(point => point.x),
                    boundsPoints.Max(point => point.y));

                if (data.sourceMax.x <= data.sourceMin.x ||
                    data.sourceMax.y <= data.sourceMin.y)
                {
                    throw new InvalidDataException("The calculated map bounds are invalid.");
                }

                return data;
            }

            private static int CountFeatureGeometries(string type, JToken coordinates)
            {
                if (coordinates == null)
                {
                    return 0;
                }

                if (type == "Polygon")
                {
                    return 1;
                }

                if (type == "MultiPolygon" || type == "MultiPoint")
                {
                    return coordinates is JArray array ? array.Count : 0;
                }

                return CountPointPairs(coordinates);
            }

            private static List<List<Vector2>> ReadOuterRings(
                string type,
                JToken coordinates)
            {
                var rings = new List<List<Vector2>>();
                if (coordinates == null)
                {
                    return rings;
                }

                if (type == "Polygon")
                {
                    AddOuterRing(coordinates, rings);
                    return rings;
                }

                if (type == "MultiPolygon" && coordinates is JArray polygons)
                {
                    foreach (var polygon in polygons)
                    {
                        AddOuterRing(polygon, rings);
                    }
                }

                return rings;
            }

            private static void ReadRoads(JArray geometries, List<VillageRoadLine> roads)
            {
                if (geometries == null)
                {
                    return;
                }

                foreach (var geometry in geometries.OfType<JObject>())
                {
                    var points = ReadPointList(geometry["coordinates"]);
                    if (points.Count < 2)
                    {
                        continue;
                    }

                    roads.Add(new VillageRoadLine(
                        points,
                        geometry.Value<float?>("width") ?? 0f));
                }
            }

            private static void AddOuterRing(JToken polygon, List<List<Vector2>> rings)
            {
                if (polygon is not JArray ringsToken || ringsToken.Count == 0)
                {
                    return;
                }

                var points = ReadPointList(ringsToken[0]);
                if (points.Count >= 3)
                {
                    rings.Add(points);
                }
            }

            private static List<Vector2> ReadPointFeatures(JToken coordinates)
            {
                var points = new List<Vector2>();
                CollectPointPairs(coordinates, points);
                return points;
            }

            private static List<Vector2> ReadPointList(JToken token)
            {
                var points = new List<Vector2>();
                if (token is not JArray pointArray)
                {
                    return points;
                }

                foreach (var point in pointArray)
                {
                    if (TryReadPoint(point, out var value))
                    {
                        points.Add(value);
                    }
                }

                return points;
            }

            private static void CollectPoints(JToken token, List<Vector2> points)
            {
                if (token == null)
                {
                    return;
                }

                if (TryReadPoint(token, out var point))
                {
                    points.Add(point);
                    return;
                }

                if (token is not JArray array)
                {
                    return;
                }

                foreach (var child in array)
                {
                    CollectPoints(child, points);
                }
            }

            private static void CollectPointPairs(JToken token, List<Vector2> points)
            {
                if (token == null)
                {
                    return;
                }

                if (TryReadPoint(token, out var point))
                {
                    points.Add(point);
                    return;
                }

                if (token is not JArray array)
                {
                    return;
                }

                foreach (var child in array)
                {
                    CollectPointPairs(child, points);
                }
            }

            private static int CountPointPairs(JToken token)
            {
                if (token == null)
                {
                    return 0;
                }

                if (TryReadPoint(token, out _))
                {
                    return 1;
                }

                if (token is not JArray array)
                {
                    return 0;
                }

                return array.Sum(CountPointPairs);
            }

            private static bool TryReadPoint(JToken token, out Vector2 point)
            {
                point = default;
                if (token is not JArray array ||
                    array.Count != 2 ||
                    !IsNumber(array[0]) ||
                    !IsNumber(array[1]))
                {
                    return false;
                }

                point = new Vector2(
                    array[0].Value<float>(),
                    array[1].Value<float>());
                return true;
            }

            private static bool IsNumber(JToken token)
            {
                return token != null &&
                       (token.Type == JTokenType.Integer ||
                        token.Type == JTokenType.Float);
            }
        }
    }
}
#endif
