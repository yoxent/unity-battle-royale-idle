#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using BattleRoyale.Maps;
using BattleRoyale.Movement;
using Newtonsoft.Json.Linq;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.AI;
using UnityEngine.Rendering;

namespace BattleRoyale.Maps.EditorTools
{
    public sealed class DungeonMapAuthoringWindow : EditorWindow
    {
        private const string VisualMaterialTemplatePath = "Assets/Materials/MapPlane.mat";
        private const int DungeonNavMeshAgentTypeId = 0;
        private const float RecommendedVoxelSizeDivisor = 3f;

        [SerializeField] private TextAsset layoutJson;
        [SerializeField] private Texture2D visualMap;
        [SerializeField] private MapDefinition mapDefinition;
        [SerializeField] private MapModeAuthoringPanel modeAuthoring = new();
        [SerializeField] private float cellWorldSize = DungeonMapGeometry.DefaultCellWorldSize;
        [SerializeField] private float baseHeight = DungeonMapGeometry.DefaultBaseHeight;
        [SerializeField] private float wallHeight = DungeonMapGeometry.DefaultWallHeight;
        [SerializeField] private float wallThickness = DungeonMapGeometry.DefaultWallThickness;
        [SerializeField] private float columnSize = DungeonMapGeometry.DefaultColumnSize;
        [SerializeField] private Vector2 columnOffset = DungeonMapGeometry.DefaultColumnOffset;
        [FormerlySerializedAs("dungeonUnitScale")]
        [SerializeField] private float unitScale = DungeonMapGeometry.DefaultUnitScale;
        [SerializeField] private bool overrideNavMeshVoxelSize;
        [SerializeField] private float navMeshVoxelSize = 0.16666667f;
        [SerializeField] private bool replaceGeneratedRoot = true;
        [SerializeField] private bool disableOtherGeneratedMaps = true;
        [SerializeField] private bool setAsActiveCameraMap = true;
        [SerializeField] private bool useImageCornerFit = true;
        [SerializeField] private Vector2 imageUpperLeftUv = new(0f, 1f);
        [SerializeField] private Vector2 imageLowerRightUv = new(1f, 0f);
        [SerializeField] private bool editImageCornersInScene = true;
        [SerializeField] private bool usesCurrentGeometryDefaults;
        [SerializeField] private bool wallThicknessDefaultIsOne;
        [SerializeField] private bool usesWallColumnAndImageDefaults;
        [SerializeField] private bool unitScaleDefaultIsOne;

        private DungeonLayoutData analyzedLayout;
        private DungeonVisualMapping visualMapping;
        private string statusMessage;
        private MessageType statusType = MessageType.Info;
        private Vector2 scrollPosition;
        private bool createDefinitionRequested;
        private bool generateRequested;
        private bool deferredActionQueued;
        private bool cornerSceneGuiRegistered;
        private bool draggingUpperLeft;
        private bool draggingLowerRight;
        private Vector3 draggedUpperLeft;
        private Vector3 draggedLowerRight;

        [MenuItem("Tools/Maps/Dungeon Map Authoring")]
        public static void Open()
        {
            Open(null);
        }

        public static void Open(MapDefinition definition)
        {
            var window = GetWindow<DungeonMapAuthoringWindow>();
            window.titleContent = new GUIContent("Dungeon Map Authoring");
            window.minSize = new Vector2(420f, 620f);
            if (definition == null)
            {
                return;
            }

            window.mapDefinition = definition;
            window.LoadDefinitionIntoWindow();
            window.Repaint();
        }

        private void OnEnable()
        {
            if (mapDefinition != null)
            {
                CopyDefinitionToWindow();
                usesCurrentGeometryDefaults = true;
                wallThicknessDefaultIsOne = true;
                usesWallColumnAndImageDefaults = true;
                unitScaleDefaultIsOne = true;
                return;
            }

            if (!unitScaleDefaultIsOne)
            {
                unitScale = DungeonMapGeometry.DefaultUnitScale;
                unitScaleDefaultIsOne = true;
            }

            if (!usesCurrentGeometryDefaults)
            {
                wallHeight = DungeonMapGeometry.DefaultWallHeight;
                wallThickness = DungeonMapGeometry.DefaultWallThickness;
                usesCurrentGeometryDefaults = true;
                wallThicknessDefaultIsOne = true;
            }
            else if (!wallThicknessDefaultIsOne)
            {
                wallThickness = DungeonMapGeometry.DefaultWallThickness;
                wallThicknessDefaultIsOne = true;
            }

            if (usesWallColumnAndImageDefaults)
            {
                return;
            }

            wallHeight = DungeonMapGeometry.DefaultWallHeight;
            columnSize = DungeonMapGeometry.DefaultColumnSize;
            columnOffset = DungeonMapGeometry.DefaultColumnOffset;
            useImageCornerFit = true;
            editImageCornersInScene = true;
            usesWallColumnAndImageDefaults = true;
        }

        private void OnDisable()
        {
            EditorApplication.delayCall -= RunDeferredActions;
            deferredActionQueued = false;
            StopCornerSceneGui();
            modeAuthoring?.Dispose();
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

            if (createDefinitionRequested)
            {
                createDefinitionRequested = false;
                CreateOrUpdateDefinition();
            }

            if (generateRequested)
            {
                generateRequested = false;
                GenerateMapRoot();
            }

            Repaint();
        }

        private void OnGUI()
        {
            AuthoringSectionHeader.DrawToolbar("Dungeon");
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            AuthoringSectionHeader.Begin("Dungeon");
            try
            {
                EditorGUILayout.HelpBox(
                    "Start with the generator JSON and PNG. Create the map, then generate its baked prefab. Set As Active Map places that prefab as the one map in the scene.",
                    MessageType.Info);
                if (!string.IsNullOrWhiteSpace(statusMessage))
                {
                    EditorGUILayout.HelpBox(statusMessage, statusType);
                }

                DrawImportSection();
                DrawMapSection();
                DrawGeometrySection();
                DrawColumnsSection();
                DrawGenerationOptionsSection();
                DrawGenerateSection();
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

        private void DrawMapSection()
        {
            if (!AuthoringSectionHeader.Draw("Map Definition"))
            {
                return;
            }

            EditorGUI.BeginChangeCheck();
            mapDefinition = (MapDefinition)EditorGUILayout.ObjectField(
                "Map Asset",
                mapDefinition,
                typeof(MapDefinition),
                false);
            if (EditorGUI.EndChangeCheck() && mapDefinition != null)
            {
                LoadDefinitionIntoWindow();
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Use Selected Asset"))
                {
                    if (Selection.activeObject is MapDefinition selected)
                    {
                        mapDefinition = selected;
                    }

                    LoadDefinitionIntoWindow();
                }

                if (GUILayout.Button("Create / Update Map Definition"))
                {
                    createDefinitionRequested = true;
                    QueueDeferredAction();
                }
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

        private void DrawGenerateSection()
        {
            if (!AuthoringSectionHeader.Draw("Generate"))
            {
                return;
            }

            using (new EditorGUI.DisabledScope(!IsMapReady()))
            {
                if (GUILayout.Button("Generate Dungeon Map Root"))
                {
                    generateRequested = true;
                    QueueDeferredAction();
                }
            }
        }

        private void DrawGeometrySection()
        {
            var values = new MapGeometrySection.Values
            {
                WorldScale = cellWorldSize,
                BaseHeight = baseHeight,
                WallHeight = wallHeight,
                WallThickness = wallThickness,
                UnitScale = unitScale
            };
            if (!MapGeometrySection.Draw(ref values, out var changed, out var applyRequested))
            {
                return;
            }

            cellWorldSize = values.WorldScale;
            baseHeight = values.BaseHeight;
            wallHeight = values.WallHeight;
            wallThickness = values.WallThickness;
            unitScale = values.UnitScale;
            if (changed)
            {
                CopyGeometryToDefinition();
            }

            if (applyRequested)
            {
                var scaledUnitCount = MapGeometrySection.ApplyToLoadedUnits(unitScale);
                EditorSceneManager.SaveOpenScenes();
                SetStatus(
                    $"Applied unit scale {unitScale:0.##} to {scaledUnitCount} unit(s).",
                    MessageType.Info);
            }

            EditorGUILayout.HelpBox(
                "Opening a saved map loads its geometry, columns, generation options, image fit, and game mode settings. A new map uses the defaults, with every game mode off.",
                MessageType.None);
        }

        private void DrawColumnsSection()
        {
            if (!AuthoringSectionHeader.Draw("Columns"))
            {
                return;
            }

            EditorGUI.BeginChangeCheck();
            columnSize = Mathf.Max(
                0.01f,
                EditorGUILayout.FloatField(
                    new GUIContent(
                        "Column Size",
                        "Cylinder diameter in world units. A cell is World Scale units wide. 6 on a scale of 20 covers 0.3 of a cell."),
                    columnSize));
            columnOffset = EditorGUILayout.Vector2Field(
                new GUIContent(
                    "Grid Offset",
                    "Generator cells added to every JSON column before the cylinder is centered. (0, 0) keeps the middle of the JSON cell. Grid Y matches the JSON and flips in the scene with the rooms."),
                columnOffset);
            var changed = EditorGUI.EndChangeCheck();
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Cell Center"))
                {
                    columnOffset = Vector2.zero;
                    changed = true;
                }

                if (GUILayout.Button("Grid Point"))
                {
                    columnOffset = DungeonMapGeometry.DefaultColumnOffset;
                    changed = true;
                }
            }

            if (analyzedLayout != null)
            {
                EditorGUILayout.LabelField("Count", analyzedLayout.columns.Count.ToString());
            }

            EditorGUILayout.HelpBox(
                "JSON columns are points. The picture draws each one as a circle, and generation builds a cylinder of this diameter and the wall height. Cell Center puts it in the middle of the JSON cell. Grid Point puts it on the JSON point, half a cell toward the grid origin. Image Alignment moves the picture, not these cylinders. Generate again after changing size or offset.",
                MessageType.None);
            if (changed)
            {
                CopyGeometryToDefinition();
            }
        }

        private void DrawGenerationOptionsSection()
        {
            if (!AuthoringSectionHeader.Draw("Generation Options"))
            {
                return;
            }

            EditorGUI.BeginChangeCheck();
            overrideNavMeshVoxelSize = EditorGUILayout.Toggle(
                new GUIContent(
                    "Override NavMesh Voxel Size",
                    "Use a custom voxel size instead of the selected NavMesh agent's project settings."),
                overrideNavMeshVoxelSize);
            using (new EditorGUI.DisabledScope(!overrideNavMeshVoxelSize))
            {
                navMeshVoxelSize = Mathf.Max(
                    0.01f,
                    EditorGUILayout.FloatField("NavMesh Voxel Size", navMeshVoxelSize));
            }

            replaceGeneratedRoot = EditorGUILayout.Toggle(
                "Replace Existing Map Prefab",
                replaceGeneratedRoot);
            setAsActiveCameraMap = EditorGUILayout.Toggle(
                new GUIContent(
                    "Set As Active Map",
                    "Saves the bake as a prefab and places that prefab on the Active Map manager. The simulation camera and solo Battle Royale both follow it. Leave this off to save the prefab without changing the map under test."),
                setAsActiveCameraMap);
            if (EditorGUI.EndChangeCheck())
            {
                CopyImageCornersToDefinition();
            }

            var recommendedVoxelSize = GetRecommendedNavMeshVoxelSize();
            EditorGUILayout.HelpBox(
                overrideNavMeshVoxelSize
                    ? $"Custom NavMesh voxel size will be limited to {recommendedVoxelSize:0.###} or less for the Humanoid agent."
                    : $"NavMesh uses the Humanoid agent's project voxel settings. Recommended voxel size: {recommendedVoxelSize:0.###}.",
                MessageType.None);
        }

        private void DrawImportSection()
        {
            if (!AuthoringSectionHeader.Draw("Import"))
            {
                return;
            }

            var previousJson = layoutJson;
            layoutJson = (TextAsset)EditorGUILayout.ObjectField(
                "Dungeon JSON",
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

            if (GUILayout.Button("Analyze JSON"))
            {
                AnalyzeLayout();
            }

            if (analyzedLayout == null)
            {
                EditorGUILayout.HelpBox(
                    "Analyze the dungeon JSON to calculate the logical bounds and inspect its room data.",
                    MessageType.None);
            }
            else
            {
                EditorGUILayout.LabelField("Title", analyzedLayout.title);
                EditorGUILayout.LabelField("Format Version", analyzedLayout.version);
                EditorGUILayout.LabelField("Source Min", analyzedLayout.sourceMin.ToString("F0"));
                EditorGUILayout.LabelField("Source Max", analyzedLayout.sourceMax.ToString("F0"));
                EditorGUILayout.LabelField(
                    "Logical Size",
                    (analyzedLayout.sourceMax - analyzedLayout.sourceMin).ToString("F0"));
                EditorGUILayout.LabelField("Rooms / Rects", analyzedLayout.rects.Count.ToString());
                EditorGUILayout.LabelField("Doors", analyzedLayout.doors.Count.ToString());
                EditorGUILayout.LabelField("Water Cells", analyzedLayout.water.Count.ToString());
                EditorGUILayout.LabelField("Columns", analyzedLayout.columns.Count.ToString());
                EditorGUILayout.LabelField("Notes", analyzedLayout.notes.Count.ToString());
            }
        }

        private void DrawImageCornerSection()
        {
            if (!AuthoringSectionHeader.Draw("Image Alignment"))
            {
                StopCornerSceneGui();
                return;
            }

            var previousFit = useImageCornerFit;
            useImageCornerFit = EditorGUILayout.Toggle(
                new GUIContent(
                    "Fit Image To Corner Points",
                    "Keep the full exported image, including the spacing around the drawing. Upper Left and Lower Right mark where the dungeon drawing sits inside that image."),
                useImageCornerFit);
            if (useImageCornerFit && !previousFit)
            {
                EnsureDefaultImageCorners();
            }

            if (!useImageCornerFit)
            {
                StopCornerSceneGui();
                RemoveImageCornerMarkers();
                if (previousFit)
                {
                    CopyImageCornersToDefinition();
                }

                return;
            }

            EditorGUI.BeginChangeCheck();
            imageUpperLeftUv = EditorGUILayout.Vector2Field(
                new GUIContent(
                    "Upper Left",
                    "Image coordinate of the dungeon drawing's top-left corner. X moves across the image. Y is 1 at the top."),
                imageUpperLeftUv);
            imageLowerRightUv = EditorGUILayout.Vector2Field(
                new GUIContent(
                    "Lower Right",
                    "Image coordinate of the dungeon drawing's bottom-right corner. X moves across the image. Y is 0 at the bottom."),
                imageLowerRightUv);
            editImageCornersInScene = EditorGUILayout.Toggle(
                new GUIContent(
                    "Edit Corner Points In Scene",
                    "Drag the two points onto the corners of the drawn dungeon. The full image, including its outer spacing, stays visible."),
                editImageCornersInScene);
            var fieldsChanged = EditorGUI.EndChangeCheck();

            if (!HasValidImageCorners())
            {
                EditorGUILayout.HelpBox(
                    "Upper Left must be above and to the left of Lower Right on the image.",
                    MessageType.Warning);
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "Drag the Upper Left and Lower Right handles in the Scene view onto the corners of the drawn dungeon, then release. The picture moves to the floor. The discs show those points in the Game view.",
                    MessageType.None);
            }

            if (GUILayout.Button("Apply Corner Points To Scene"))
            {
                fieldsChanged = true;
            }

            if (fieldsChanged || previousFit != useImageCornerFit)
            {
                CopyImageCornersToDefinition();
                ApplyImageCornerFitToScene();
            }

            UpdateCornerSceneGui();
            if (!draggingUpperLeft && !draggingLowerRight)
            {
                SyncImageCornerMarkers();
            }
        }

        private void AnalyzeLayout()
        {
            if (layoutJson == null)
            {
                SetStatus("Assign a dungeon JSON before analyzing.", MessageType.Error);
                return;
            }

            var json = layoutJson.text?.TrimStart('\uFEFF', ' ', '\t', '\r', '\n');
            if (string.IsNullOrEmpty(json) || json[0] != '{')
            {
                analyzedLayout = null;
                SetStatus(
                    $"{layoutJson.name} is not a dungeon JSON file. Assign the Watabou JSON export. " +
                    "The markdown writeup is a description and cannot be analyzed.",
                    MessageType.Error);
                return;
            }

            try
            {
                analyzedLayout = DungeonLayoutReader.Read(json);
                if (visualMap == null)
                {
                    visualMap = FindSiblingVisualMap(layoutJson);
                }

                SetStatus(
                    $"Analyzed {layoutJson.name}: {analyzedLayout.rects.Count} rooms found.",
                    MessageType.Info);
            }
            catch (Exception exception)
            {
                analyzedLayout = null;
                Debug.LogException(exception);
                SetStatus(
                    $"Could not parse {layoutJson.name}: {exception.Message}",
                    MessageType.Error);
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
                    "Save the dungeon JSON in the project before creating its Definitions folder.",
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
                    DungeonMapAuthoring.ApplyNewMapDefaults(mapDefinition);
                    AssetDatabase.CreateAsset(mapDefinition, assetPath);
                    ApplyGeometry(DungeonMapGeometry.Read(mapDefinition));
                    ApplyAuthoring(DungeonMapAuthoring.Read(mapDefinition));
                    modeAuthoring?.BindSavedSettings(mapDefinition);
                }
                else
                {
                    CopyDefinitionToWindow();
                    modeAuthoring?.BindSavedSettings(mapDefinition);
                }
            }

            mapDefinition.layoutJson = layoutJson;
            var narrativeMarkdown = MapModeAuthoringPanel.FindSiblingMarkdown(layoutJson);
            if (narrativeMarkdown != null)
            {
                mapDefinition.narrativeMarkdown = narrativeMarkdown;
            }
            mapDefinition.visualMap = visualMap;
            mapDefinition.visualSprite = MapModeAuthoringPanel.SpriteForVisualMap(visualMap);
            DungeonMapGeometry.Write(mapDefinition, CurrentGeometry());
            mapDefinition.sourceMin = analyzedLayout.sourceMin;
            mapDefinition.sourceMax = analyzedLayout.sourceMax;
            mapDefinition.heightScale = 0f;
            CopyImageCornersToDefinition();
            mapDefinition.generateBuildingWalls = false;
            mapDefinition.generateTreeColliders = false;
            mapDefinition.generator = "dungeon";
            mapDefinition.generatorVersion = analyzedLayout.version;
            mapDefinition.features = CreateFeatureSummaries(analyzedLayout);
            mapDefinition.visualMaterial = GetOrCreateVisualMaterial();

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
                SetStatus("Stop Play Mode before generating a dungeon map root.", MessageType.Error);
                return;
            }

            if (!IsMapReady())
            {
                SetStatus("Create a map definition before generating a dungeon map root.", MessageType.Error);
                return;
            }

            if (analyzedLayout == null ||
                analyzedLayout.sourceMin != mapDefinition.sourceMin ||
                analyzedLayout.sourceMax != mapDefinition.sourceMax)
            {
                layoutJson = mapDefinition.layoutJson;
                visualMap = mapDefinition.visualMap;
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
            Undo.RegisterCreatedObjectUndo(root, "Generate Dungeon Map Root");
            var mapInstance = root.AddComponent<MapInstance>();
            mapInstance.definition = mapDefinition;

            CopyGeometryToDefinition();
            CopyImageCornersToDefinition();
            if (useImageCornerFit)
            {
                visualMapping = default;
            }
            else
            {
                MeasureVisualMapping();
            }

            CreateVisualMap(root.transform);
            if (useImageCornerFit)
            {
                SyncImageCornerMarkers(root.transform);
            }

            CreateWalls(root.transform);
            CreateBoundaryWalls(root.transform);
            CreateNavigation(root);
            CreateFloorCollider(root.transform);
            modeAuthoring.GenerateMarkers(root.transform, mapDefinition);
            var scaledUnitCount = MapGeometrySection.ApplyToLoadedUnits(unitScale);
            var placed = MapBakeOutput.Commit(root, mapDefinition, setAsActiveCameraMap);
            if (setAsActiveCameraMap && placed == null)
            {
                SetStatus(
                    "The dungeon was built, but its prefab was not placed as the active map.",
                    MessageType.Error);
                return;
            }

            SetStatus(
                setAsActiveCameraMap
                    ? $"Saved {prefabPath} and placed it as the active map. " +
                      $"{analyzedLayout.FloorCells.Count} walkable cells. " +
                      $"Applied unit scale {unitScale:0.##} to {scaledUnitCount} unit(s)."
                    : $"Saved {prefabPath}. The scene map was left unchanged. " +
                      $"{analyzedLayout.FloorCells.Count} walkable cells.",
                MessageType.Info);
        }

        private void CreateVisualMap(Transform parent)
        {
            var visualObject = new GameObject("VisualMap");
            visualObject.transform.SetParent(parent, false);
            visualObject.transform.position = new Vector3(
                0f,
                baseHeight - 0.02f,
                0f);

            var meshFilter = visualObject.AddComponent<MeshFilter>();
            var meshRenderer = visualObject.AddComponent<MeshRenderer>();
            meshFilter.sharedMesh = BuildBoundsQuad();
            meshRenderer.sharedMaterial = GetOrCreateVisualMaterial();
        }

        private void CreateFloorCollider(Transform parent)
        {
            var floorObject = new GameObject("DungeonFloor");
            floorObject.transform.SetParent(parent, false);
            floorObject.layer = LayerMask.NameToLayer("MapGround");

            var corners = GetVisualFloorCorners();
            DungeonFloorBox.GetCenterAndSize(
                corners[0],
                corners[1],
                corners[2],
                corners[3],
                baseHeight,
                DungeonFloorBox.Thickness,
                out var center,
                out var size);
            var collider = floorObject.AddComponent<BoxCollider>();
            collider.center = center;
            collider.size = size;

            var modifier = floorObject.AddComponent<NavMeshModifier>();
            modifier.ignoreFromBuild = true;
        }

        private Vector3[] GetVisualFloorCorners()
        {
            if (useImageCornerFit && HasValidImageCorners())
            {
                return new[]
                {
                    ImageUvToLocal(new Vector2(0f, 0f)),
                    ImageUvToLocal(new Vector2(0f, 1f)),
                    ImageUvToLocal(new Vector2(1f, 1f)),
                    ImageUvToLocal(new Vector2(1f, 0f))
                };
            }

            var min = analyzedLayout.sourceMin;
            var max = analyzedLayout.sourceMax;
            return new[]
            {
                GridToWorld(min.x, max.y, 0f),
                GridToWorld(min.x, min.y, 0f),
                GridToWorld(max.x, min.y, 0f),
                GridToWorld(max.x, max.y, 0f)
            };
        }

        private void CreateWalls(Transform parent)
        {
            var wallRoot = new GameObject("DungeonWalls");
            wallRoot.transform.SetParent(parent, false);
            wallRoot.layer = LayerMask.NameToLayer("MapObstacle");

            var edges = new List<DungeonWallEdge>();
            foreach (var cell in analyzedLayout.FloorCells)
            {
                TryAddWallEdge(edges, cell, new Vector2Int(0, -1), true, cell.y, cell.x);
                TryAddWallEdge(edges, cell, new Vector2Int(0, 1), true, cell.y + 1, cell.x);
                TryAddWallEdge(edges, cell, new Vector2Int(-1, 0), false, cell.x, cell.y);
                TryAddWallEdge(edges, cell, new Vector2Int(1, 0), false, cell.x + 1, cell.y);
            }

            var runs = new List<DungeonWallRun>();
            DungeonWallRuns.Merge(edges, runs);
            AddWallMesh(wallRoot.transform, runs);
        }

        private void CreateBoundaryWalls(Transform parent)
        {
            if (!TryVisualRect(out var min, out var max))
            {
                return;
            }

            var height = wallHeight > 0.01f ? wallHeight : 2f;
            var thickness = wallThickness > 0.01f ? wallThickness : 1f;
            MapBoundaryWalls.Create(
                parent,
                $"{mapDefinition.name}_BoundaryWalls",
                min,
                max,
                thickness,
                height,
                baseHeight);
        }

        private bool TryVisualRect(out Vector2 min, out Vector2 max)
        {
            return MapPlayBounds.TryGetRect(GetVisualFloorCorners(), out min, out max);
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

        private void TryAddWallEdge(
            List<DungeonWallEdge> edges,
            Vector2Int cell,
            Vector2Int neighborOffset,
            bool horizontal,
            int boundary,
            int segment)
        {
            var neighbor = cell + neighborOffset;
            if (analyzedLayout.FloorCells.Contains(neighbor) ||
                analyzedLayout.DoorBoundaries.Contains(
                    new BoundaryKey(horizontal, boundary, segment)))
            {
                return;
            }

            edges.Add(new DungeonWallEdge(horizontal, boundary, segment));
        }

        private void AddWallMesh(Transform parent, List<DungeonWallRun> runs)
        {
            var mesh = BuildWallMesh(runs);
            if (mesh.vertexCount == 0)
            {
                DestroyImmediate(mesh);
                return;
            }

            var visual = new GameObject("DungeonWallMesh");
            visual.transform.SetParent(parent, false);
            visual.layer = LayerMask.NameToLayer("MapObstacle");
            var meshFilter = visual.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = mesh;
            var meshCollider = visual.AddComponent<MeshCollider>();
            meshCollider.sharedMesh = mesh;
            meshCollider.convex = false;
            var meshRenderer = visual.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = WallBoxMesh.SharedMaterial();
        }

        private Mesh BuildWallMesh(List<DungeonWallRun> runs)
        {
            var mesh = new Mesh
            {
                name = $"{mapDefinition.name}_DungeonWalls"
            };
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            var halfThickness = wallThickness * 0.5f;
            var clipToMap = TryInnerRect(halfThickness, out var innerMin, out var innerMax);
            for (var i = 0; i < runs.Count; i++)
            {
                var run = runs[i];
                var start = run.Horizontal
                    ? GridToWorld(run.SegmentStart, run.Boundary, 0f)
                    : GridToWorld(run.Boundary, run.SegmentStart, 0f);
                var end = run.Horizontal
                    ? GridToWorld(run.SegmentStart + run.Length, run.Boundary, 0f)
                    : GridToWorld(run.Boundary, run.SegmentStart + run.Length, 0f);
                var clippedStart = new Vector2(start.x, start.z);
                var clippedEnd = new Vector2(end.x, end.z);
                if (clipToMap &&
                    !MapPlayBounds.TryClipSegment(
                        clippedStart,
                        clippedEnd,
                        innerMin,
                        innerMax,
                        out clippedStart,
                        out clippedEnd))
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
                var horizontal = Mathf.Abs(edge.x) >= Mathf.Abs(edge.y);
                var size = horizontal
                    ? new Vector3(length, wallHeight, wallThickness)
                    : new Vector3(wallThickness, wallHeight, length);
                WallBoxMesh.AppendBox(
                    vertices,
                    triangles,
                    new Vector3(midpoint.x, baseHeight + wallHeight * 0.5f, midpoint.y),
                    size,
                    Quaternion.identity);
            }

            AppendColumns(vertices, triangles);
            AppendRotundaWalls(vertices, triangles);

            if (vertices.Count > 65535)
            {
                mesh.indexFormat = IndexFormat.UInt32;
            }

            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private void AppendColumns(List<Vector3> vertices, List<int> triangles)
        {
            var keepColumnsInside = TryInnerRect(columnSize * 0.5f, out var columnMin, out var columnMax);
            var boxes = new List<DungeonColumnBox>();
            DungeonColumnBoxes.Collect(
                analyzedLayout.columns,
                columnSize,
                columnOffset,
                wallHeight,
                baseHeight,
                keepColumnsInside,
                columnMin,
                columnMax,
                GridToWorld,
                boxes);
            for (var i = 0; i < boxes.Count; i++)
            {
                WallBoxMesh.AppendCylinder(
                    vertices,
                    triangles,
                    boxes[i].Center,
                    boxes[i].Size.x,
                    boxes[i].Size.y,
                    WallBoxMesh.ColumnSides);
            }
        }

        private void AppendRotundaWalls(List<Vector3> vertices, List<int> triangles)
        {
            var clip = TryInnerRect(wallThickness * 0.5f, out var innerMin, out var innerMax);
            foreach (var rect in analyzedLayout.rects)
            {
                if (!rect.rotunda)
                {
                    continue;
                }

                var starts = new List<Vector2>();
                var ends = new List<Vector2>();
                CollectRotundaWalls(rect, starts, ends);
                for (var i = 0; i < starts.Count; i++)
                {
                    var worldStart = GridToWorld(starts[i].x, starts[i].y, 0f);
                    var worldEnd = GridToWorld(ends[i].x, ends[i].y, 0f);
                    var clippedStart = new Vector2(worldStart.x, worldStart.z);
                    var clippedEnd = new Vector2(worldEnd.x, worldEnd.z);
                    if (clip &&
                        !MapPlayBounds.TryClipSegment(
                            clippedStart,
                            clippedEnd,
                            innerMin,
                            innerMax,
                            out clippedStart,
                            out clippedEnd))
                    {
                        continue;
                    }

                    var edge = clippedEnd - clippedStart;
                    var length = edge.magnitude + wallThickness;
                    if (length <= 0.001f)
                    {
                        continue;
                    }

                    var midpoint = (clippedStart + clippedEnd) * 0.5f;
                    var rotation = Quaternion.LookRotation(
                        new Vector3(edge.x, 0f, edge.y).normalized,
                        Vector3.up);
                    WallBoxMesh.AppendBox(
                        vertices,
                        triangles,
                        new Vector3(midpoint.x, baseHeight + wallHeight * 0.5f, midpoint.y),
                        new Vector3(wallThickness, wallHeight, length),
                        rotation);
                }
            }
        }

        private void AppendRotundaFloors(
            List<Vector3> vertices,
            List<Vector2> uvs,
            List<int> triangles,
            Vector2 sourceSize)
        {
            foreach (var rect in analyzedLayout.rects)
            {
                if (!rect.rotunda)
                {
                    continue;
                }

                var loop = BuildRotundaLoop(rect);
                if (loop.Count < 3)
                {
                    continue;
                }

                var centerIndex = vertices.Count;
                var centerX = rect.x + rect.width * 0.5f;
                var centerY = rect.y + rect.height * 0.5f;
                vertices.Add(GridToWorld(centerX, centerY, baseHeight));
                uvs.Add(ToBoundsUv(new Vector2(centerX, centerY), sourceSize));
                var rimStart = vertices.Count;
                for (var i = 0; i < loop.Count; i++)
                {
                    var point = loop[i].Position;
                    vertices.Add(GridToWorld(point.x, point.y, baseHeight));
                    uvs.Add(ToBoundsUv(point, sourceSize));
                }

                for (var i = 0; i < loop.Count; i++)
                {
                    var next = (i + 1) % loop.Count;
                    triangles.Add(centerIndex);
                    triangles.Add(rimStart + next);
                    triangles.Add(rimStart + i);
                }
            }
        }

        private void CollectRotundaWalls(
            DungeonRect rect,
            List<Vector2> starts,
            List<Vector2> ends)
        {
            DungeonRotundaGeometry.AppendWallSegments(BuildRotundaLoop(rect), starts, ends);
        }

        private List<DungeonRotundaGeometry.OutlinePoint> BuildRotundaLoop(DungeonRect rect)
        {
            var openings = new List<DungeonRotundaGeometry.Opening>();
            for (var i = 0; i < analyzedLayout.doors.Count; i++)
            {
                var door = analyzedLayout.doors[i];
                DungeonRotundaGeometry.CollectOpenings(
                    door.x,
                    door.y,
                    rect.x,
                    rect.y,
                    rect.width,
                    rect.height,
                    openings);
            }

            var loop = new List<DungeonRotundaGeometry.OutlinePoint>();
            DungeonRotundaGeometry.BuildOutline(
                rect.x + rect.width * 0.5f,
                rect.y + rect.height * 0.5f,
                rect.width * 0.5f,
                rect.height * 0.5f,
                openings,
                DungeonRotundaGeometry.DefaultSegments,
                loop);
            return loop;
        }

        private void CreateNavigation(GameObject root)
        {
            var floorMesh = BuildFloorMesh();
            var navigationFloor = new GameObject("DungeonFloorNav");
            navigationFloor.transform.SetParent(root.transform, false);
            navigationFloor.layer = LayerMask.NameToLayer("MapGround");
            var navigationFilter = navigationFloor.AddComponent<MeshFilter>();
            var navigationCollider = navigationFloor.AddComponent<MeshCollider>();
            navigationFilter.sharedMesh = floorMesh;
            navigationCollider.sharedMesh = floorMesh;

            var surface = root.AddComponent<NavMeshSurface>();
            surface.agentTypeID = DungeonNavMeshAgentTypeId;
            surface.collectObjects = CollectObjects.Children;
            surface.layerMask = (1 << LayerMask.NameToLayer("MapGround")) |
                                (1 << LayerMask.NameToLayer("MapObstacle"));
            surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            surface.overrideVoxelSize = overrideNavMeshVoxelSize;
            if (overrideNavMeshVoxelSize)
            {
                navMeshVoxelSize = GetSafeNavMeshVoxelSize();
                surface.voxelSize = navMeshVoxelSize;
            }

            var navigation = root.AddComponent<MapNavigationController>();
            var serialized = new SerializedObject(navigation);
            serialized.FindProperty("surface").objectReferenceValue = surface;
            serialized.FindProperty("rebuildOnAwake").boolValue = false;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            try
            {
                navigation.Rebuild();
            }
            finally
            {
                DestroyImmediate(navigationFloor);
                DestroyImmediate(floorMesh);
            }
        }

        private float GetSafeNavMeshVoxelSize()
        {
            return Mathf.Min(
                Mathf.Max(0.01f, navMeshVoxelSize),
                GetRecommendedNavMeshVoxelSize());
        }

        private static float GetRecommendedNavMeshVoxelSize()
        {
            var agentSettings = NavMesh.GetSettingsByID(DungeonNavMeshAgentTypeId);
            var agentRadius = agentSettings.agentRadius;
            if (agentRadius <= 0f)
            {
                return 0.16666667f;
            }

            return agentRadius / RecommendedVoxelSizeDivisor;
        }

        private Mesh BuildFloorMesh()
        {
            var mesh = new Mesh
            {
                name = $"{mapDefinition.name}_DungeonFloor"
            };
            var vertices = new List<Vector3>();
            var uvs = new List<Vector2>();
            var triangles = new List<int>();
            var sourceSize = analyzedLayout.sourceMax - analyzedLayout.sourceMin;

            foreach (var cell in analyzedLayout.FloorCells)
            {
                var vertexStart = vertices.Count;
                var bottomLeft = GridToWorld(cell.x, cell.y + 1f, baseHeight);
                var topLeft = GridToWorld(cell.x, cell.y, baseHeight);
                var topRight = GridToWorld(cell.x + 1f, cell.y, baseHeight);
                var bottomRight = GridToWorld(cell.x + 1f, cell.y + 1f, baseHeight);

                vertices.Add(bottomLeft);
                vertices.Add(topLeft);
                vertices.Add(topRight);
                vertices.Add(bottomRight);

                uvs.Add(ToBoundsUv(new Vector2(cell.x, cell.y), sourceSize));
                uvs.Add(ToBoundsUv(new Vector2(cell.x, cell.y + 1), sourceSize));
                uvs.Add(ToBoundsUv(new Vector2(cell.x + 1, cell.y + 1), sourceSize));
                uvs.Add(ToBoundsUv(new Vector2(cell.x + 1, cell.y), sourceSize));

                triangles.Add(vertexStart);
                triangles.Add(vertexStart + 1);
                triangles.Add(vertexStart + 2);
                triangles.Add(vertexStart);
                triangles.Add(vertexStart + 2);
                triangles.Add(vertexStart + 3);
            }

            AppendRotundaFloors(vertices, uvs, triangles, sourceSize);

            if (vertices.Count > 65535)
            {
                mesh.indexFormat = IndexFormat.UInt32;
            }

            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private Mesh BuildBoundsQuad()
        {
            if (useImageCornerFit && HasValidImageCorners())
            {
                return BuildFullImageQuad();
            }

            var mesh = new Mesh
            {
                name = $"{mapDefinition.name}_Visual"
            };
            var min = analyzedLayout.sourceMin;
            var max = analyzedLayout.sourceMax;
            mesh.vertices = new[]
            {
                GridToWorld(min.x, max.y, 0f),
                GridToWorld(min.x, min.y, 0f),
                GridToWorld(max.x, min.y, 0f),
                GridToWorld(max.x, max.y, 0f)
            };
            mesh.uv = new[]
            {
                GridToVisualUv(min.x, max.y),
                GridToVisualUv(min.x, min.y),
                GridToVisualUv(max.x, min.y),
                GridToVisualUv(max.x, max.y)
            };
            mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private Mesh BuildFullImageQuad()
        {
            var mesh = new Mesh
            {
                name = $"{mapDefinition.name}_Visual"
            };
            mesh.vertices = new[]
            {
                ImageUvToLocal(new Vector2(0f, 0f)),
                ImageUvToLocal(new Vector2(0f, 1f)),
                ImageUvToLocal(new Vector2(1f, 1f)),
                ImageUvToLocal(new Vector2(1f, 0f))
            };
            mesh.uv = new[]
            {
                new Vector2(0f, 0f),
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                new Vector2(1f, 0f)
            };
            mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private Vector3 ImageUvToLocal(Vector2 imageUv)
        {
            var upperLeft = GridToWorld(
                analyzedLayout.sourceMin.x,
                analyzedLayout.sourceMin.y,
                0f);
            var lowerRight = GridToWorld(
                analyzedLayout.sourceMax.x,
                analyzedLayout.sourceMax.y,
                0f);
            return new Vector3(
                Mathf.LerpUnclamped(
                    upperLeft.x,
                    lowerRight.x,
                    InverseLerpUnclamped(
                        imageUpperLeftUv.x,
                        imageLowerRightUv.x,
                        imageUv.x)),
                0f,
                Mathf.LerpUnclamped(
                    upperLeft.z,
                    lowerRight.z,
                    InverseLerpUnclamped(
                        imageUpperLeftUv.y,
                        imageLowerRightUv.y,
                        imageUv.y)));
        }

        private Vector2 WorldToImageUv(Vector3 worldPosition)
        {
            var visual = FindVisualMap();
            var local = visual != null
                ? visual.InverseTransformPoint(worldPosition)
                : worldPosition;
            var upperLeft = GridToWorld(
                analyzedLayout.sourceMin.x,
                analyzedLayout.sourceMin.y,
                0f);
            var lowerRight = GridToWorld(
                analyzedLayout.sourceMax.x,
                analyzedLayout.sourceMax.y,
                0f);
            return new Vector2(
                Mathf.LerpUnclamped(
                    imageUpperLeftUv.x,
                    imageLowerRightUv.x,
                    InverseLerpUnclamped(upperLeft.x, lowerRight.x, local.x)),
                Mathf.LerpUnclamped(
                    imageUpperLeftUv.y,
                    imageLowerRightUv.y,
                    InverseLerpUnclamped(upperLeft.z, lowerRight.z, local.z)));
        }

        private Vector3 ImageUvToWorld(Vector2 imageUv)
        {
            var local = ImageUvToLocal(imageUv);
            var visual = FindVisualMap();
            if (visual == null)
            {
                return local + new Vector3(0f, baseHeight - 0.02f, 0f);
            }

            return visual.TransformPoint(local);
        }

        private void ApplyImageCornerFitToScene()
        {
            if (!useImageCornerFit ||
                !HasValidImageCorners() ||
                mapDefinition == null)
            {
                return;
            }

            if (analyzedLayout == null)
            {
                AnalyzeLayout();
            }

            if (analyzedLayout == null)
            {
                return;
            }

            var visual = FindVisualMap();
            if (visual == null)
            {
                SetStatus(
                    "Generate the dungeon map before applying image corner points.",
                    MessageType.Warning);
                return;
            }

            var meshFilter = visual.GetComponent<MeshFilter>();
            if (meshFilter == null)
            {
                return;
            }

            var previousMesh = meshFilter.sharedMesh;
            meshFilter.sharedMesh = BuildFullImageQuad();
            if (previousMesh != null && !AssetDatabase.Contains(previousMesh))
            {
                DestroyImmediate(previousMesh);
            }

            EditorSceneManager.MarkSceneDirty(visual.gameObject.scene);
            SyncImageCornerMarkers();
            SetStatus(
                "Fit the full image between the upper-left and lower-right corner points.",
                MessageType.Info);
        }

        private void CopyImageCornersToDefinition()
        {
            if (mapDefinition == null)
            {
                return;
            }

            EnsureDefaultImageCorners();
            DungeonMapAuthoring.Write(mapDefinition, CurrentAuthoring());
            EditorUtility.SetDirty(mapDefinition);
            AssetDatabase.SaveAssets();
        }

        private void EnsureDefaultImageCorners()
        {
            if (HasValidImageCorners())
            {
                return;
            }

            imageUpperLeftUv = new Vector2(0f, 1f);
            imageLowerRightUv = new Vector2(1f, 0f);
        }

        private bool HasValidImageCorners()
        {
            return imageLowerRightUv.x > imageUpperLeftUv.x + 0.001f &&
                   imageUpperLeftUv.y > imageLowerRightUv.y + 0.001f;
        }

        private static float InverseLerpUnclamped(float from, float to, float value)
        {
            var span = to - from;
            if (Mathf.Abs(span) <= 0.0001f)
            {
                return 0f;
            }

            return (value - from) / span;
        }

        private void UpdateCornerSceneGui()
        {
            var shouldRegister = editImageCornersInScene && useImageCornerFit;
            if (shouldRegister == cornerSceneGuiRegistered)
            {
                return;
            }

            if (shouldRegister)
            {
                SceneView.duringSceneGui += OnCornerSceneGui;
                cornerSceneGuiRegistered = true;
            }
            else
            {
                StopCornerSceneGui();
            }

            SceneView.RepaintAll();
        }

        private void StopCornerSceneGui()
        {
            if (!cornerSceneGuiRegistered)
            {
                return;
            }

            SceneView.duringSceneGui -= OnCornerSceneGui;
            cornerSceneGuiRegistered = false;
            draggingUpperLeft = false;
            draggingLowerRight = false;
        }

        private void OnCornerSceneGui(SceneView sceneView)
        {
            if (!useImageCornerFit || !editImageCornersInScene)
            {
                return;
            }

            if (analyzedLayout == null)
            {
                AnalyzeLayout();
            }

            if (analyzedLayout == null || FindVisualMap() == null)
            {
                Handles.BeginGUI();
                GUILayout.BeginArea(new Rect(12f, 12f, 360f, 48f));
                EditorGUILayout.HelpBox(
                    "Generate this dungeon map to place the image corner points.",
                    MessageType.Info);
                GUILayout.EndArea();
                Handles.EndGUI();
                return;
            }

            DrawImageCornerHandle(
                "Upper Left",
                UpperLeftMarkerName,
                new Color(1f, 0.85f, 0.2f),
                ref draggingUpperLeft,
                ref draggedUpperLeft,
                imageUpperLeftUv,
                ApplyDraggedUpperLeft);
            DrawImageCornerHandle(
                "Lower Right",
                LowerRightMarkerName,
                new Color(0.3f, 0.75f, 1f),
                ref draggingLowerRight,
                ref draggedLowerRight,
                imageLowerRightUv,
                ApplyDraggedLowerRight);
        }

        private void DrawImageCornerHandle(
            string label,
            string markerName,
            Color color,
            ref bool dragging,
            ref Vector3 draggedPosition,
            Vector2 currentUv,
            Action<Vector3> applyDrag)
        {
            var position = dragging
                ? draggedPosition
                : ImageUvToWorld(currentUv);
            Handles.color = color;
            Handles.SphereHandleCap(
                0,
                position,
                Quaternion.identity,
                HandleUtility.GetHandleSize(position) * 0.15f,
                EventType.Repaint);
            Handles.Label(position + Vector3.up * 1.5f, label);

            EditorGUI.BeginChangeCheck();
            var moved = Handles.PositionHandle(position, Quaternion.identity);
            if (EditorGUI.EndChangeCheck())
            {
                dragging = true;
                draggedPosition = moved;
                PlaceImageCornerMarker(FindGeneratedMapRoot(), markerName, moved, color);
                EditorApplication.QueuePlayerLoopUpdate();
            }

            if (!dragging || Event.current.rawType != EventType.MouseUp)
            {
                return;
            }

            dragging = false;
            applyDrag(draggedPosition);
        }

        private void ApplyDraggedUpperLeft(Vector3 worldPosition)
        {
            TryApplyDraggedCorner(worldPosition, true);
        }

        private void ApplyDraggedLowerRight(Vector3 worldPosition)
        {
            TryApplyDraggedCorner(worldPosition, false);
        }

        private void TryApplyDraggedCorner(Vector3 worldPosition, bool upperLeft)
        {
            var updated = WorldToImageUv(worldPosition);
            var previousUpperLeft = imageUpperLeftUv;
            var previousLowerRight = imageLowerRightUv;
            if (upperLeft)
            {
                imageUpperLeftUv = updated;
            }
            else
            {
                imageLowerRightUv = updated;
            }

            if (HasValidImageCorners())
            {
                FinishImageCornerDrag();
                return;
            }

            imageUpperLeftUv = previousUpperLeft;
            imageLowerRightUv = previousLowerRight;
            SyncImageCornerMarkers();
            SetStatus(
                "Upper Left must stay above and to the left of Lower Right.",
                MessageType.Warning);
            Repaint();
        }

        private void FinishImageCornerDrag()
        {
            if (!HasValidImageCorners())
            {
                SetStatus(
                    "Upper Left must stay above and to the left of Lower Right.",
                    MessageType.Warning);
                Repaint();
                return;
            }

            CopyImageCornersToDefinition();
            ApplyImageCornerFitToScene();
            Repaint();
            SceneView.RepaintAll();
        }

        private const string UpperLeftMarkerName = "ImageCornerUpperLeft";
        private const string LowerRightMarkerName = "ImageCornerLowerRight";
        private const string CornerDiscName = "MarkerDisc";
        private const float CornerMarkerDiameter = 12f;
        private const string CornerMarkerMaterialPath = "Assets/Materials/MarkerCube.mat";

        private static Mesh cornerDiscMesh;

        private void SyncImageCornerMarkers(Transform explicitRoot = null)
        {
            if (!useImageCornerFit ||
                !HasValidImageCorners() ||
                mapDefinition == null)
            {
                return;
            }

            if (analyzedLayout == null)
            {
                AnalyzeLayout();
            }

            var root = explicitRoot != null ? explicitRoot : FindGeneratedMapRoot();
            if (analyzedLayout == null || root == null)
            {
                return;
            }

            var changed = PlaceImageCornerMarker(
                root,
                UpperLeftMarkerName,
                ImageUvToWorld(imageUpperLeftUv),
                new Color(1f, 0.85f, 0.2f));
            changed |= PlaceImageCornerMarker(
                root,
                LowerRightMarkerName,
                ImageUvToWorld(imageLowerRightUv),
                new Color(0.3f, 0.75f, 1f));
            if (!changed)
            {
                return;
            }

            EditorSceneManager.MarkSceneDirty(root.gameObject.scene);
        }

        private bool PlaceImageCornerMarker(
            Transform root,
            string markerName,
            Vector3 worldPosition,
            Color color)
        {
            if (root == null)
            {
                return false;
            }

            var marker = root.Find(markerName);
            var created = marker == null;
            if (created)
            {
                var markerObject = new GameObject(markerName);
                Undo.RegisterCreatedObjectUndo(markerObject, "Show Image Corner");
                markerObject.transform.SetParent(root, true);
                AddCornerDisc(markerObject.transform, color);
                marker = markerObject.transform;
            }

            var moved = marker.position != worldPosition;
            if (moved)
            {
                marker.position = worldPosition;
            }

            return created || moved;
        }

        private void RemoveImageCornerMarkers()
        {
            var root = FindGeneratedMapRoot();
            if (root == null)
            {
                return;
            }

            var removed = RemoveCornerMarker(root, UpperLeftMarkerName);
            removed |= RemoveCornerMarker(root, LowerRightMarkerName);
            if (removed)
            {
                EditorSceneManager.MarkSceneDirty(root.gameObject.scene);
            }
        }

        private static bool RemoveCornerMarker(Transform root, string markerName)
        {
            var marker = root.Find(markerName);
            if (marker == null)
            {
                return false;
            }

            Undo.DestroyObjectImmediate(marker.gameObject);
            return true;
        }

        private static void AddCornerDisc(Transform marker, Color color)
        {
            var disc = new GameObject(CornerDiscName);
            disc.transform.SetParent(marker, false);
            disc.transform.localPosition = new Vector3(0f, 0.4f, 0f);
            disc.transform.localScale = new Vector3(
                CornerMarkerDiameter,
                0.04f,
                CornerMarkerDiameter);
            var meshFilter = disc.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = GetCornerDiscMesh();
            var renderer = disc.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(
                CornerMarkerMaterialPath);
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            var block = new MaterialPropertyBlock();
            block.SetColor("_BaseColor", color);
            block.SetColor("_Color", color);
            renderer.SetPropertyBlock(block);
        }

        private static Mesh GetCornerDiscMesh()
        {
            if (cornerDiscMesh != null)
            {
                return cornerDiscMesh;
            }

            var temp = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cornerDiscMesh = temp.GetComponent<MeshFilter>().sharedMesh;
            UnityEngine.Object.DestroyImmediate(temp);
            return cornerDiscMesh;
        }

        private Transform FindGeneratedMapRoot()
        {
            if (mapDefinition == null)
            {
                return null;
            }

            var manager = ActiveMapManager.Find();
            if (manager != null &&
                manager.ActiveMap != null &&
                manager.ActiveMap.definition == mapDefinition)
            {
                return manager.ActiveMap.transform;
            }

            var maps = UnityEngine.Object.FindObjectsByType<MapInstance>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            foreach (var map in maps)
            {
                if (map != null &&
                    map.definition == mapDefinition &&
                    map.gameObject.scene.IsValid())
                {
                    return map.transform;
                }
            }

            var rootName = $"Map_{mapDefinition.name.Replace("_Map", string.Empty)}";
            var root = FindSceneObject(rootName);
            return root == null ? null : root.transform;
        }

        private Transform FindVisualMap()
        {
            var root = FindGeneratedMapRoot();
            return root == null ? null : root.Find("VisualMap");
        }

        private Vector3 GridToWorld(float gridX, float gridY, float height)
        {
            // Watabou grid Y grows downward from the top-left. Reflect it
            // through the map center so the top of the exported image stays
            // at the top of the top-down view, without moving that center.
            var flippedGridY = analyzedLayout.sourceMin.y +
                               analyzedLayout.sourceMax.y -
                               gridY;
            return new Vector3(
                gridX * cellWorldSize,
                height,
                flippedGridY * cellWorldSize);
        }

        private Vector2 GridToVisualUv(float gridX, float gridY)
        {
            var sourceMin = analyzedLayout.sourceMin;
            var sourceMax = analyzedLayout.sourceMax;
            if (!visualMapping.valid)
            {
                return new Vector2(
                    Mathf.InverseLerp(sourceMin.x, sourceMax.x, gridX),
                    Mathf.InverseLerp(sourceMax.y, sourceMin.y, gridY));
            }

            var pixelX = visualMapping.originPixelX +
                         (gridX - sourceMin.x) * visualMapping.cellPixelSize;
            var pixelY = visualMapping.originPixelTop -
                         (gridY - sourceMin.y) * visualMapping.cellPixelSize;
            return new Vector2(
                pixelX / visualMapping.textureWidth,
                pixelY / visualMapping.textureHeight);
        }

        private void MeasureVisualMapping()
        {
            visualMapping = default;
            if (visualMap == null || analyzedLayout == null)
            {
                return;
            }

            var sourceSize = analyzedLayout.sourceMax - analyzedLayout.sourceMin;
            if (sourceSize.x <= 0f || sourceSize.y <= 0f)
            {
                return;
            }

            var readable = new Texture2D(
                visualMap.width,
                visualMap.height,
                TextureFormat.RGBA32,
                false);
            var renderTexture = RenderTexture.GetTemporary(
                visualMap.width,
                visualMap.height,
                0,
                RenderTextureFormat.ARGB32);
            Graphics.Blit(visualMap, renderTexture);
            var previous = RenderTexture.active;
            RenderTexture.active = renderTexture;
            readable.ReadPixels(
                new Rect(0f, 0f, visualMap.width, visualMap.height),
                0,
                0);
            readable.Apply();
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(renderTexture);

            var minX = readable.width;
            var minY = readable.height;
            var maxX = 0;
            var maxY = 0;
            var found = false;
            var pixels = readable.GetPixels32();
            var width = readable.width;
            var height = readable.height;
            for (var y = 0; y < height; y += 2)
            {
                var row = y * width;
                for (var x = 0; x < width; x += 2)
                {
                    var pixel = pixels[row + x];
                    var luminance =
                        (pixel.r * 0.3f + pixel.g * 0.59f + pixel.b * 0.11f) /
                        255f;
                    if (luminance >= 0.62f)
                    {
                        continue;
                    }

                    found = true;
                    if (x < minX)
                    {
                        minX = x;
                    }

                    if (y < minY)
                    {
                        minY = y;
                    }

                    if (x > maxX)
                    {
                        maxX = x;
                    }

                    if (y > maxY)
                    {
                        maxY = y;
                    }
                }
            }

            DestroyImmediate(readable);
            if (!found || maxX <= minX || maxY <= minY)
            {
                return;
            }

            var cellPixelX = (maxX - minX) / sourceSize.x;
            var cellPixelY = (maxY - minY) / sourceSize.y;
            if (cellPixelX < 1f || cellPixelY < 1f)
            {
                return;
            }

            var aspectError = Mathf.Abs(cellPixelX - cellPixelY) /
                              Mathf.Max(cellPixelX, cellPixelY);
            if (aspectError > 0.2f)
            {
                return;
            }

            var wallCenters = CollectWallCenters();
            var baseCell = (cellPixelX + cellPixelY) * 0.5f;
            var bestScore = -1f;
            var bestCell = baseCell;
            var bestOriginX = (minX + maxX) * 0.5f - sourceSize.x * 0.5f * baseCell;
            var bestOriginTop = (minY + maxY) * 0.5f + sourceSize.y * 0.5f * baseCell;
            if (wallCenters.Count > 0)
            {
                for (var cellStep = -4; cellStep <= 4; cellStep++)
                {
                    var cell = baseCell + cellStep * 0.5f;
                    if (cell < 1f)
                    {
                        continue;
                    }

                    for (var originX = minX - 20; originX <= minX + 20; originX += 2)
                    {
                        for (var originTop = maxY - 20; originTop <= maxY + 20; originTop += 2)
                        {
                            var score = ScoreWallInk(
                                pixels,
                                width,
                                height,
                                wallCenters,
                                sourceMinX: analyzedLayout.sourceMin.x,
                                sourceMinY: analyzedLayout.sourceMin.y,
                                originX,
                                originTop,
                                cell);
                            if (score > bestScore)
                            {
                                bestScore = score;
                                bestCell = cell;
                                bestOriginX = originX;
                                bestOriginTop = originTop;
                            }
                        }
                    }
                }
            }

            visualMapping = new DungeonVisualMapping
            {
                valid = true,
                originPixelX = bestOriginX,
                originPixelTop = bestOriginTop,
                cellPixelSize = bestCell,
                textureWidth = width,
                textureHeight = height
            };
        }

        private List<Vector3> CollectWallCenters()
        {
            var centers = new List<Vector3>();
            foreach (var cell in analyzedLayout.FloorCells)
            {
                TryAddWallCenter(
                    centers,
                    cell,
                    new Vector2Int(0, -1),
                    true,
                    cell.y,
                    cell.x);
                TryAddWallCenter(
                    centers,
                    cell,
                    new Vector2Int(0, 1),
                    true,
                    cell.y + 1,
                    cell.x);
                TryAddWallCenter(
                    centers,
                    cell,
                    new Vector2Int(-1, 0),
                    false,
                    cell.x,
                    cell.y);
                TryAddWallCenter(
                    centers,
                    cell,
                    new Vector2Int(1, 0),
                    false,
                    cell.x + 1,
                    cell.y);
            }

            AppendRotundaSamples(centers);
            return centers;
        }

        private void AppendRotundaSamples(List<Vector3> centers)
        {
            foreach (var rect in analyzedLayout.rects)
            {
                if (!rect.rotunda)
                {
                    continue;
                }

                var starts = new List<Vector2>();
                var ends = new List<Vector2>();
                CollectRotundaWalls(rect, starts, ends);
                for (var i = 0; i < starts.Count; i++)
                {
                    var mid = (starts[i] + ends[i]) * 0.5f;
                    centers.Add(new Vector3(mid.x, mid.y, 0f));
                }
            }
        }

        private void TryAddWallCenter(
            List<Vector3> centers,
            Vector2Int cell,
            Vector2Int neighborOffset,
            bool horizontal,
            int boundary,
            int segment)
        {
            var neighbor = cell + neighborOffset;
            if (analyzedLayout.FloorCells.Contains(neighbor) ||
                analyzedLayout.DoorBoundaries.Contains(
                    new BoundaryKey(horizontal, boundary, segment)))
            {
                return;
            }

            centers.Add(horizontal
                ? new Vector3(segment + 0.5f, boundary, 1f)
                : new Vector3(boundary, segment + 0.5f, 0f));
        }

        private static float ScoreWallInk(
            Color32[] pixels,
            int width,
            int height,
            List<Vector3> wallCenters,
            float sourceMinX,
            float sourceMinY,
            int originX,
            int originTop,
            float cell)
        {
            var darkness = 0f;
            var samples = 0;
            for (var i = 0; i < wallCenters.Count; i += 2)
            {
                var center = wallCenters[i];
                var pixelX = originX + (center.x - sourceMinX) * cell;
                var pixelY = originTop - (center.y - sourceMinY) * cell;
                var horizontal = center.z > 0.5f;
                var worst = 1f;
                for (var band = -4; band <= 4; band += 2)
                {
                    var x = Mathf.RoundToInt(horizontal ? pixelX : pixelX + band);
                    var y = Mathf.RoundToInt(horizontal ? pixelY + band : pixelY);
                    if (x < 0 || y < 0 || x >= width || y >= height)
                    {
                        continue;
                    }

                    var pixel = pixels[y * width + x];
                    var luminance =
                        (pixel.r * 0.3f + pixel.g * 0.59f + pixel.b * 0.11f) /
                        255f;
                    if (luminance < worst)
                    {
                        worst = luminance;
                    }
                }

                darkness += 1f - worst;
                samples++;
            }

            return samples == 0 ? 0f : darkness / samples;
        }

        private Vector2 ToBoundsUv(Vector2 source, Vector2 sourceSize)
        {
            return new Vector2(
                Mathf.InverseLerp(
                    analyzedLayout.sourceMin.x,
                    analyzedLayout.sourceMax.x,
                    source.x),
                Mathf.InverseLerp(
                    analyzedLayout.sourceMin.y,
                    analyzedLayout.sourceMax.y,
                    source.y));
        }

        private Material GetOrCreateVisualMaterial()
        {
            if (mapDefinition == null)
            {
                return AssetDatabase.LoadAssetAtPath<Material>(VisualMaterialTemplatePath);
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
                    return AssetDatabase.LoadAssetAtPath<Material>(VisualMaterialTemplatePath);
                }

                var materialPath = $"{definitionsFolder}/{mapName}_Visual.mat";
                material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);

                if (material == null)
                {
                    var template = AssetDatabase.LoadAssetAtPath<Material>(
                        VisualMaterialTemplatePath);
                    material = template != null
                        ? new Material(template)
                        : new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    material.name = $"{mapName}_Visual";
                    ConfigureCreatedVisualMaterial(material);
                    AssetDatabase.CreateAsset(material, materialPath);
                }
            }

            if (material.HasProperty("_BaseMap"))
            {
                material.SetTexture("_BaseMap", visualMap);
            }

            if (material.HasProperty("_MainTex"))
            {
                material.SetTexture("_MainTex", visualMap);
            }

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", Color.white);
            }

            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", Color.white);
            }

            mapDefinition.visualMaterial = material;
            EditorUtility.SetDirty(material);
            EditorUtility.SetDirty(mapDefinition);
            AssetDatabase.SaveAssets();
            return material;
        }

        private static void ConfigureCreatedVisualMaterial(Material material)
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

        private static MapFeatureSummary[] CreateFeatureSummaries(
            DungeonLayoutData layout)
        {
            return new[]
            {
                new MapFeatureSummary
                {
                    id = "rects",
                    type = "Rooms",
                    geometryCount = layout.rects.Count,
                    coordinateCount = layout.rects.Count * 4
                },
                new MapFeatureSummary
                {
                    id = "doors",
                    type = "Doors",
                    geometryCount = layout.doors.Count,
                    coordinateCount = layout.doors.Count
                },
                new MapFeatureSummary
                {
                    id = "water",
                    type = "BlockedCells",
                    geometryCount = layout.water.Count,
                    coordinateCount = layout.water.Count
                },
                new MapFeatureSummary
                {
                    id = "columns",
                    type = "Columns",
                    geometryCount = layout.columns.Count,
                    coordinateCount = layout.columns.Count
                }
            };
        }

        private bool IsMapReady()
        {
            return mapDefinition != null &&
                   mapDefinition.layoutJson != null &&
                   mapDefinition.HasBounds;
        }

        private DungeonMapGeometrySettings CurrentGeometry()
        {
            return new DungeonMapGeometrySettings(
                cellWorldSize,
                baseHeight,
                wallHeight,
                wallThickness,
                columnSize,
                unitScale,
                columnOffset);
        }

        private void ApplyGeometry(DungeonMapGeometrySettings geometry)
        {
            cellWorldSize = geometry.CellWorldSize;
            baseHeight = geometry.BaseHeight;
            wallHeight = geometry.WallHeight;
            wallThickness = geometry.WallThickness;
            columnSize = geometry.ColumnSize;
            unitScale = geometry.UnitScale;
            columnOffset = geometry.ColumnOffset;
        }

        private void CopyGeometryToDefinition()
        {
            if (mapDefinition == null)
            {
                return;
            }

            DungeonMapGeometry.Write(mapDefinition, CurrentGeometry());
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

            CopyDefinitionToWindow();
            modeAuthoring?.BindSavedSettings(mapDefinition);
            AnalyzeLayout();
        }

        private void CopyDefinitionToWindow()
        {
            layoutJson = mapDefinition.layoutJson;
            visualMap = mapDefinition.visualMap;
            ApplyGeometry(DungeonMapGeometry.Read(mapDefinition));
            ApplyAuthoring(DungeonMapAuthoring.Read(mapDefinition));
            EnsureDefaultImageCorners();
        }

        private void ApplyAuthoring(DungeonMapAuthoringSettings settings)
        {
            overrideNavMeshVoxelSize = settings.OverrideNavMeshVoxelSize;
            navMeshVoxelSize = settings.NavMeshVoxelSize;
            replaceGeneratedRoot = settings.ReplaceGeneratedRoot;
            disableOtherGeneratedMaps = settings.DisableOtherGeneratedMaps;
            setAsActiveCameraMap = settings.SetAsActiveCameraMap;
            useImageCornerFit = settings.UseImageCornerFit;
            imageUpperLeftUv = settings.ImageUpperLeftUv;
            imageLowerRightUv = settings.ImageLowerRightUv;
            editImageCornersInScene = settings.EditImageCornersInScene;
        }

        private DungeonMapAuthoringSettings CurrentAuthoring()
        {
            var editPoints = mapDefinition == null || mapDefinition.editPointsInSceneView;
            return new DungeonMapAuthoringSettings(
                overrideNavMeshVoxelSize,
                navMeshVoxelSize,
                replaceGeneratedRoot,
                disableOtherGeneratedMaps,
                setAsActiveCameraMap,
                useImageCornerFit,
                imageUpperLeftUv,
                imageLowerRightUv,
                editImageCornersInScene,
                editPoints);
        }

        private static Texture2D FindSiblingVisualMap(TextAsset jsonAsset)
        {
            if (jsonAsset == null)
            {
                return null;
            }

            var jsonPath = AssetDatabase.GetAssetPath(jsonAsset);
            return AssetDatabase.LoadAssetAtPath<Texture2D>(
                Path.ChangeExtension(jsonPath, ".png"));
        }

        private static GameObject FindSceneObject(string objectName)
        {
            foreach (var candidate in Resources.FindObjectsOfTypeAll<GameObject>())
            {
                if (candidate.name == objectName &&
                    candidate.scene.IsValid() &&
                    candidate.scene.isLoaded)
                {
                    return candidate;
                }
            }

            return null;
        }

        private void SetStatus(string message, MessageType type)
        {
            statusMessage = message;
            statusType = type;
        }

        private struct DungeonVisualMapping
        {
            public bool valid;
            public float originPixelX;
            public float originPixelTop;
            public float cellPixelSize;
            public int textureWidth;
            public int textureHeight;
        }

        private sealed class DungeonLayoutData
        {
            public string version;
            public string title;
            public Vector2 sourceMin;
            public Vector2 sourceMax;
            public readonly List<DungeonRect> rects = new();
            public readonly List<DungeonDoor> doors = new();
            public readonly HashSet<Vector2Int> water = new();
            public readonly List<Vector2Int> columns = new();
            public readonly List<DungeonNote> notes = new();
            public readonly HashSet<Vector2Int> FloorCells = new();
            public readonly HashSet<BoundaryKey> DoorBoundaries = new();
        }

        private sealed class DungeonRect
        {
            public int x;
            public int y;
            public int width;
            public int height;
            public bool rotunda;
        }

        private sealed class DungeonDoor
        {
            public int x;
            public int y;
            public Vector2Int direction;
        }

        private sealed class DungeonNote
        {
            public string text;
            public string reference;
        }

        private readonly struct BoundaryKey : IEquatable<BoundaryKey>
        {
            private readonly bool horizontal;
            private readonly int boundary;
            private readonly int segment;

            public BoundaryKey(bool horizontal, int boundary, int segment)
            {
                this.horizontal = horizontal;
                this.boundary = boundary;
                this.segment = segment;
            }

            public bool Equals(BoundaryKey other)
            {
                return horizontal == other.horizontal &&
                       boundary == other.boundary &&
                       segment == other.segment;
            }

            public override bool Equals(object obj)
            {
                return obj is BoundaryKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    var hash = horizontal ? 1 : 0;
                    hash = hash * 397 ^ boundary;
                    hash = hash * 397 ^ segment;
                    return hash;
                }
            }
        }

        private static class DungeonLayoutReader
        {
            public static DungeonLayoutData Read(string json)
            {
                var root = JObject.Parse(json);
                var rectTokens = root["rects"] as JArray;
                if (rectTokens == null || rectTokens.Count == 0)
                {
                    throw new InvalidDataException(
                        "The dungeon JSON does not contain a non-empty rects array.");
                }

                var data = new DungeonLayoutData
                {
                    version = root.Value<string>("version") ?? string.Empty,
                    title = root.Value<string>("title") ?? string.Empty
                };

                var minX = int.MaxValue;
                var minY = int.MaxValue;
                var maxX = int.MinValue;
                var maxY = int.MinValue;

                foreach (var token in rectTokens)
                {
                    var rect = new DungeonRect
                    {
                        x = RequiredInt(token, "x"),
                        y = RequiredInt(token, "y"),
                        width = RequiredInt(token, "w"),
                        height = RequiredInt(token, "h"),
                        rotunda = token.Value<bool?>("rotunda") ?? false
                    };
                    if (rect.width <= 0 || rect.height <= 0)
                    {
                        throw new InvalidDataException("Dungeon rects must have positive size.");
                    }

                    data.rects.Add(rect);
                    minX = Mathf.Min(minX, rect.x);
                    minY = Mathf.Min(minY, rect.y);
                    maxX = Mathf.Max(maxX, rect.x + rect.width);
                    maxY = Mathf.Max(maxY, rect.y + rect.height);
                    if (rect.rotunda)
                    {
                        continue;
                    }

                    for (var y = rect.y; y < rect.y + rect.height; y++)
                    {
                        for (var x = rect.x; x < rect.x + rect.width; x++)
                        {
                            data.FloorCells.Add(new Vector2Int(x, y));
                        }
                    }
                }

                ReadWater(root["water"], data, ref minX, ref minY, ref maxX, ref maxY);
                ReadColumns(root["columns"], data, ref minX, ref minY, ref maxX, ref maxY);
                ReadDoors(root["doors"], data);
                ReadNotes(root["notes"], data);

                foreach (var waterCell in data.water)
                {
                    data.FloorCells.Remove(waterCell);
                }

                foreach (var door in data.doors)
                {
                    AddDoorBoundary(data, door);
                }

                data.sourceMin = new Vector2(minX, minY);
                data.sourceMax = new Vector2(maxX, maxY);
                if (data.sourceMax.x <= data.sourceMin.x ||
                    data.sourceMax.y <= data.sourceMin.y)
                {
                    throw new InvalidDataException("The calculated dungeon bounds are invalid.");
                }

                return data;
            }

            private static void ReadWater(
                JToken token,
                DungeonLayoutData data,
                ref int minX,
                ref int minY,
                ref int maxX,
                ref int maxY)
            {
                if (!(token is JArray array))
                {
                    return;
                }

                foreach (var point in array)
                {
                    if (!TryReadGridPoint(point, out var cell))
                    {
                        continue;
                    }

                    data.water.Add(cell);
                    IncludeCell(cell, ref minX, ref minY, ref maxX, ref maxY);
                }
            }

            private static void ReadColumns(
                JToken token,
                DungeonLayoutData data,
                ref int minX,
                ref int minY,
                ref int maxX,
                ref int maxY)
            {
                if (!(token is JArray array))
                {
                    return;
                }

                foreach (var point in array)
                {
                    if (!TryReadGridPoint(point, out var cell))
                    {
                        continue;
                    }

                    data.columns.Add(cell);
                    IncludeCell(cell, ref minX, ref minY, ref maxX, ref maxY);
                }
            }

            private static void ReadDoors(JToken token, DungeonLayoutData data)
            {
                if (!(token is JArray array))
                {
                    return;
                }

                foreach (var doorToken in array)
                {
                    if (!(doorToken is JObject doorObject) ||
                        !TryReadGridPoint(doorObject, out var position))
                    {
                        continue;
                    }

                    var direction = doorObject["dir"] as JObject;
                    if (direction == null)
                    {
                        continue;
                    }

                    data.doors.Add(new DungeonDoor
                    {
                        x = position.x,
                        y = position.y,
                        direction = new Vector2Int(
                            direction.Value<int?>("x") ?? 0,
                            direction.Value<int?>("y") ?? 0)
                    });
                }
            }

            private static void ReadNotes(JToken token, DungeonLayoutData data)
            {
                if (!(token is JArray array))
                {
                    return;
                }

                foreach (var noteToken in array)
                {
                    if (!(noteToken is JObject noteObject))
                    {
                        continue;
                    }

                    data.notes.Add(new DungeonNote
                    {
                        text = noteObject.Value<string>("text") ?? string.Empty,
                        reference = noteObject.Value<string>("ref") ?? string.Empty
                    });
                }
            }

            private static void AddDoorBoundary(
                DungeonLayoutData data,
                DungeonDoor door)
            {
                if (door.direction.x > 0)
                {
                    data.DoorBoundaries.Add(
                        new BoundaryKey(false, door.x + 1, door.y));
                }
                else if (door.direction.x < 0)
                {
                    data.DoorBoundaries.Add(
                        new BoundaryKey(false, door.x, door.y));
                }
                else if (door.direction.y > 0)
                {
                    data.DoorBoundaries.Add(
                        new BoundaryKey(true, door.y + 1, door.x));
                }
                else if (door.direction.y < 0)
                {
                    data.DoorBoundaries.Add(
                        new BoundaryKey(true, door.y, door.x));
                }

                for (var rectIndex = 0; rectIndex < data.rects.Count; rectIndex++)
                {
                    var rect = data.rects[rectIndex];
                    if (!rect.rotunda)
                    {
                        continue;
                    }

                    var openings = new List<DungeonRotundaGeometry.Opening>();
                    DungeonRotundaGeometry.CollectOpenings(
                        door.x,
                        door.y,
                        rect.x,
                        rect.y,
                        rect.width,
                        rect.height,
                        openings);
                    for (var openingIndex = 0; openingIndex < openings.Count; openingIndex++)
                    {
                        var opening = openings[openingIndex];
                        data.DoorBoundaries.Add(new BoundaryKey(
                            !opening.Vertical,
                            Mathf.RoundToInt(opening.Edge),
                            Mathf.RoundToInt(opening.SpanStart)));
                    }
                }
            }

            private static bool TryReadGridPoint(
                JToken token,
                out Vector2Int point)
            {
                point = default;
                if (token is JArray array &&
                    array.Count >= 2 &&
                    IsNumber(array[0]) &&
                    IsNumber(array[1]))
                {
                    point = new Vector2Int(
                        array[0].Value<int>(),
                        array[1].Value<int>());
                    return true;
                }

                if (token is JObject objectToken &&
                    IsNumber(objectToken["x"]) &&
                    IsNumber(objectToken["y"]))
                {
                    point = new Vector2Int(
                        objectToken.Value<int>("x"),
                        objectToken.Value<int>("y"));
                    return true;
                }

                return false;
            }

            private static int RequiredInt(JToken token, string propertyName)
            {
                if (!IsNumber(token[propertyName]))
                {
                    throw new InvalidDataException(
                        $"Dungeon rect is missing numeric '{propertyName}'.");
                }

                return token.Value<int>(propertyName);
            }

            private static bool IsNumber(JToken token)
            {
                return token != null &&
                       (token.Type == JTokenType.Integer ||
                        token.Type == JTokenType.Float);
            }

            private static void IncludeCell(
                Vector2Int cell,
                ref int minX,
                ref int minY,
                ref int maxX,
                ref int maxY)
            {
                minX = Mathf.Min(minX, cell.x);
                minY = Mathf.Min(minY, cell.y);
                maxX = Mathf.Max(maxX, cell.x + 1);
                maxY = Mathf.Max(maxY, cell.y + 1);
            }
        }
    }
}
#endif
