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
    public sealed class MapAuthoringWindow : EditorWindow
    {
        private const string DefinitionsFolder = "Assets/Maps/Definitions";
        private const string VisualMaterialTemplatePath = "Assets/Materials/MapPlane.mat";
        private const int MaximumGeneratedSurfaceResolution = 513;

        private enum HeightPaintMode
        {
            Raise,
            Lower,
            Smooth,
            Flatten
        }

        [SerializeField] private TextAsset layoutJson;
        [SerializeField] private Texture2D visualMap;
        [SerializeField] private MapDefinition mapDefinition;
        [SerializeField] private MapModeAuthoringPanel modeAuthoring = new();
        [SerializeField] private float worldScale = 1f;
        [SerializeField] private float baseHeight;
        [SerializeField] private float heightScale = 20f;
        [SerializeField] private int heightResolution = 129;
        [SerializeField] private float wallHeight = 3f;
        [SerializeField] private float wallThickness = 0.7f;
        [SerializeField] private bool generateBuildingWalls = true;
        [SerializeField] private bool generateTreeColliders;
        [SerializeField] private bool replaceGeneratedRoot = true;
        [SerializeField] private HeightPaintMode heightPaintMode;
        [SerializeField] private float brushRadiusPixels = 20f;
        [SerializeField] private float brushStrength = 0.05f;
        [SerializeField] private float flattenValue = 0.5f;

        private LayoutData analyzedLayout;
        private string statusMessage;
        private MessageType statusType = MessageType.Info;
        private Vector2 scrollPosition;
        private bool paintingHeight;

        [MenuItem("Tools/Maps/Village Map Authoring")]
        public static void Open()
        {
            var window = GetWindow<MapAuthoringWindow>();
            window.titleContent = new GUIContent("Village Map Authoring");
            window.minSize = new Vector2(420f, 600f);
        }

        private void OnDisable()
        {
            modeAuthoring?.Dispose();
        }

        private void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            EditorGUILayout.HelpBox(
                "Import a handpicked map export, create a reusable map definition, paint optional terrain height, and generate invisible collision geometry.",
                MessageType.Info);

            DrawSourceSection();
            DrawSettingsSection();
            DrawAnalysisSection();
            DrawAssetSection();
            modeAuthoring ??= new MapModeAuthoringPanel();
            modeAuthoring.Draw(mapDefinition, DefinitionsFolder);
            DrawHeightSection();
            DrawGenerationSection();

            EditorGUILayout.EndScrollView();
        }

        private void DrawSourceSection()
        {
            EditorGUILayout.LabelField("Source", EditorStyles.boldLabel);

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

            EditorGUILayout.Space(8f);
        }

        private void DrawSettingsSection()
        {
            EditorGUILayout.LabelField("Map Settings", EditorStyles.boldLabel);

            worldScale = Mathf.Max(
                0.0001f,
                EditorGUILayout.FloatField("World Scale", worldScale));
            baseHeight = EditorGUILayout.FloatField("Base Height", baseHeight);
            heightScale = Mathf.Max(
                0f,
                EditorGUILayout.FloatField("Maximum Terrain Height", heightScale));
            heightResolution = Mathf.Clamp(
                EditorGUILayout.IntField("Height Resolution", heightResolution),
                2,
                MaximumGeneratedSurfaceResolution);
            wallHeight = Mathf.Max(
                0f,
                EditorGUILayout.FloatField("Wall Height", wallHeight));
            wallThickness = Mathf.Max(
                0.001f,
                EditorGUILayout.FloatField("Wall Thickness", wallThickness));
            EditorGUI.BeginChangeCheck();
            generateBuildingWalls = EditorGUILayout.Toggle(
                "Generate Building Walls",
                generateBuildingWalls);
            generateTreeColliders = EditorGUILayout.Toggle(
                "Generate Tree Colliders",
                generateTreeColliders);
            replaceGeneratedRoot = EditorGUILayout.Toggle(
                "Replace Existing Generated Root",
                replaceGeneratedRoot);
            if (EditorGUI.EndChangeCheck())
            {
                CopyGenerationOptionsToDefinition();
            }

            EditorGUILayout.Space(8f);
        }

        private void DrawAnalysisSection()
        {
            EditorGUILayout.LabelField("Analysis", EditorStyles.boldLabel);

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

            EditorGUILayout.Space(8f);
        }

        private void DrawAssetSection()
        {
            EditorGUILayout.LabelField("Map Definition", EditorStyles.boldLabel);
            mapDefinition = (MapDefinition)EditorGUILayout.ObjectField(
                "Map Asset",
                mapDefinition,
                typeof(MapDefinition),
                false);

            if (GUILayout.Button("Create / Update Map Definition"))
            {
                CreateOrUpdateDefinition();
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
                EditorGUILayout.ObjectField(
                    "Assigned Height",
                    mapDefinition.heightMap,
                    typeof(Texture2D),
                    false);
            }

            EditorGUILayout.Space(8f);
        }

        private void DrawHeightSection()
        {
            EditorGUILayout.LabelField("Height Authoring", EditorStyles.boldLabel);

            if (!IsMapReady())
            {
                EditorGUILayout.HelpBox(
                    "Create a map definition before creating or painting height data.",
                    MessageType.None);
                return;
            }

            if (GUILayout.Button("Create / Reset Flat Height Map"))
            {
                CreateFlatHeightMap();
            }

            if (mapDefinition.heightMap == null)
            {
                EditorGUILayout.HelpBox(
                    "No height map assigned. Generated terrain will remain flat at Base Height.",
                    MessageType.None);
                return;
            }

            heightPaintMode = (HeightPaintMode)EditorGUILayout.EnumPopup(
                "Paint Mode",
                heightPaintMode);
            brushRadiusPixels = Mathf.Clamp(
                EditorGUILayout.FloatField("Brush Radius (pixels)", brushRadiusPixels),
                1f,
                256f);
            brushStrength = Mathf.Clamp01(
                EditorGUILayout.FloatField("Brush Strength", brushStrength));
            flattenValue = Mathf.Clamp01(
                EditorGUILayout.FloatField("Flatten Value", flattenValue));

            EditorGUILayout.HelpBox(
                "Paint directly on the height map preview. Height values are normalized and multiplied by Maximum Terrain Height when the map is generated.",
                MessageType.None);

            DrawHeightPainter();

            if (GUILayout.Button("Save Height Map"))
            {
                SaveHeightMap();
            }

            EditorGUILayout.Space(8f);
        }

        private void DrawGenerationSection()
        {
            EditorGUILayout.LabelField("Generation", EditorStyles.boldLabel);

            using (new EditorGUI.DisabledScope(!IsMapReady()))
            {
                if (GUILayout.Button("Generate Map Root"))
                {
                    GenerateMapRoot();
                }
            }

            if (!string.IsNullOrWhiteSpace(statusMessage))
            {
                EditorGUILayout.HelpBox(statusMessage, statusType);
            }
        }

        private void DrawHeightPainter()
        {
            var heightMap = mapDefinition.heightMap;
            if (heightMap == null)
            {
                return;
            }

            var availableWidth = Mathf.Max(1f, position.width - 40f);
            var aspect = heightMap.width / (float)Mathf.Max(1, heightMap.height);
            var previewWidth = availableWidth;
            var previewHeight = previewWidth / aspect;
            var previewRect = GUILayoutUtility.GetRect(
                previewWidth,
                previewHeight,
                GUILayout.ExpandWidth(false));

            if (visualMap != null)
            {
                GUI.DrawTexture(previewRect, visualMap, ScaleMode.ScaleToFit, false);
                var previousColor = GUI.color;
                GUI.color = new Color(1f, 1f, 1f, 0.55f);
                GUI.DrawTexture(previewRect, heightMap, ScaleMode.ScaleToFit, false);
                GUI.color = previousColor;
            }
            else
            {
                GUI.DrawTexture(previewRect, heightMap, ScaleMode.ScaleToFit, false);
            }

            HandleHeightPaint(previewRect, heightMap);
        }

        private void HandleHeightPaint(Rect previewRect, Texture2D heightMap)
        {
            var currentEvent = Event.current;
            var isPaintingEvent =
                currentEvent.type == EventType.MouseDown ||
                currentEvent.type == EventType.MouseDrag;

            if (isPaintingEvent &&
                currentEvent.button == 0 &&
                previewRect.Contains(currentEvent.mousePosition))
            {
                if (currentEvent.type == EventType.MouseDown)
                {
                    Undo.RecordObject(heightMap, "Paint Map Height");
                    paintingHeight = true;
                }

                var uv = new Vector2(
                    Mathf.InverseLerp(previewRect.xMin, previewRect.xMax, currentEvent.mousePosition.x),
                    1f - Mathf.InverseLerp(
                        previewRect.yMin,
                        previewRect.yMax,
                        currentEvent.mousePosition.y));
                PaintHeightAt(heightMap, uv);
                currentEvent.Use();
                Repaint();
            }
            else if (currentEvent.type == EventType.MouseUp && paintingHeight)
            {
                paintingHeight = false;
                SaveHeightMap();
            }
        }

        private void PaintHeightAt(Texture2D heightMap, Vector2 uv)
        {
            var centerX = Mathf.RoundToInt(uv.x * (heightMap.width - 1));
            var centerY = Mathf.RoundToInt(uv.y * (heightMap.height - 1));
            var radius = Mathf.CeilToInt(brushRadiusPixels);
            var radiusSquared = radius * radius;

            var minX = Mathf.Max(0, centerX - radius);
            var maxX = Mathf.Min(heightMap.width - 1, centerX + radius);
            var minY = Mathf.Max(0, centerY - radius);
            var maxY = Mathf.Min(heightMap.height - 1, centerY + radius);

            for (var y = minY; y <= maxY; y++)
            {
                for (var x = minX; x <= maxX; x++)
                {
                    var dx = x - centerX;
                    var dy = y - centerY;
                    var distanceSquared = dx * dx + dy * dy;
                    if (distanceSquared > radiusSquared)
                    {
                        continue;
                    }

                    var distance = Mathf.Sqrt(distanceSquared);
                    var falloff = 1f - distance / radius;
                    var current = heightMap.GetPixel(x, y).r;
                    var next = current;

                    switch (heightPaintMode)
                    {
                        case HeightPaintMode.Raise:
                            next = current + brushStrength * falloff;
                            break;
                        case HeightPaintMode.Lower:
                            next = current - brushStrength * falloff;
                            break;
                        case HeightPaintMode.Flatten:
                            next = Mathf.Lerp(current, flattenValue, brushStrength * falloff);
                            break;
                        case HeightPaintMode.Smooth:
                            next = Mathf.Lerp(current, SampleAverage(heightMap, x, y), brushStrength * falloff);
                            break;
                    }

                    var value = Mathf.Clamp01(next);
                    heightMap.SetPixel(x, y, new Color(value, value, value, 1f));
                }
            }

            heightMap.Apply(false, false);
            EditorUtility.SetDirty(heightMap);
            EditorUtility.SetDirty(mapDefinition);
        }

        private static float SampleAverage(Texture2D texture, int centerX, int centerY)
        {
            var total = 0f;
            var count = 0;

            for (var y = -1; y <= 1; y++)
            {
                for (var x = -1; x <= 1; x++)
                {
                    var sampleX = Mathf.Clamp(centerX + x, 0, texture.width - 1);
                    var sampleY = Mathf.Clamp(centerY + y, 0, texture.height - 1);
                    total += texture.GetPixel(sampleX, sampleY).r;
                    count++;
                }
            }

            return total / count;
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

            EnsureDefinitionsFolder();

            if (mapDefinition == null)
            {
                var mapName = Path.GetFileNameWithoutExtension(layoutJson.name);
                var assetPath = AssetDatabase.GenerateUniqueAssetPath(
                    $"{DefinitionsFolder}/{mapName}_Map.asset");
                mapDefinition = CreateInstance<MapDefinition>();
                mapDefinition.name = $"{mapName}_Map";
                AssetDatabase.CreateAsset(mapDefinition, assetPath);
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
            mapDefinition.worldScale = worldScale;
            mapDefinition.baseHeight = baseHeight;
            mapDefinition.heightScale = heightScale;
            mapDefinition.heightResolution = heightResolution;
            mapDefinition.wallHeight = wallHeight;
            mapDefinition.wallThickness = wallThickness;
            mapDefinition.generateBuildingWalls = generateBuildingWalls;
            mapDefinition.generateTreeColliders = generateTreeColliders;
            mapDefinition.replaceGeneratedRoot = replaceGeneratedRoot;
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

        private void CreateFlatHeightMap()
        {
            if (!IsMapReady())
            {
                SetStatus("Create a map definition before creating a height map.", MessageType.Error);
                return;
            }

            EnsureDefinitionsFolder();
            var resolution = Mathf.Clamp(
                mapDefinition.heightResolution,
                2,
                MaximumGeneratedSurfaceResolution);
            var mapName = mapDefinition.name.Replace("_Map", string.Empty);
            var path = $"{DefinitionsFolder}/{mapName}_Height.asset";
            var heightMap = AssetDatabase.LoadAssetAtPath<Texture2D>(path);

            if (heightMap == null || heightMap.width != resolution || heightMap.height != resolution)
            {
                path = AssetDatabase.GenerateUniqueAssetPath(path);
                heightMap = new Texture2D(
                    resolution,
                    resolution,
                    TextureFormat.RGBA32,
                    false,
                    true);
                heightMap.name = $"{mapName}_Height";
                AssetDatabase.CreateAsset(heightMap, path);
            }

            var flatPixels = Enumerable.Repeat(
                new Color(0f, 0f, 0f, 1f),
                resolution * resolution).ToArray();
            heightMap.SetPixels(flatPixels);
            heightMap.Apply(false, false);
            mapDefinition.heightMap = heightMap;
            EditorUtility.SetDirty(heightMap);
            EditorUtility.SetDirty(mapDefinition);
            AssetDatabase.SaveAssets();

            SetStatus(
                $"Flat {resolution}x{resolution} height map created.",
                MessageType.Info);
        }

        private void SaveHeightMap()
        {
            if (mapDefinition == null || mapDefinition.heightMap == null)
            {
                return;
            }

            mapDefinition.heightMap.Apply(false, false);
            EditorUtility.SetDirty(mapDefinition.heightMap);
            EditorUtility.SetDirty(mapDefinition);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            SetStatus("Height map saved.", MessageType.Info);
        }

        private void GenerateMapRoot()
        {
            if (!IsMapReady())
            {
                SetStatus("Create a map definition before generating a map root.", MessageType.Error);
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

            var rootName = $"Map_{mapDefinition.name.Replace("_Map", string.Empty)}";
            var existingRoot = GameObject.Find(rootName);
            if (existingRoot != null)
            {
                if (!replaceGeneratedRoot)
                {
                    SetStatus(
                        $"A generated root named {rootName} already exists.",
                        MessageType.Warning);
                    Selection.activeGameObject = existingRoot;
                    return;
                }

                Undo.DestroyObjectImmediate(existingRoot);
            }

            var root = new GameObject(rootName);
            Undo.RegisterCreatedObjectUndo(root, "Generate Map Root");
            var mapInstance = root.AddComponent<MapInstance>();
            mapInstance.definition = mapDefinition;

            var surface = CreateSurface(root.transform);
            CreateBuildingWalls(root.transform);

            if (generateTreeColliders)
            {
                CreateTreeColliders(root.transform);
            }

            modeAuthoring.GenerateMarkers(root.transform, mapDefinition);
            EditorSceneManager.MarkSceneDirty(root.scene);
            EditorSceneManager.SaveOpenScenes();
            Selection.activeGameObject = root;
            SetStatus(
                $"Generated {rootName} with {surface.vertexCount} terrain vertices.",
                MessageType.Info);
        }

        private Mesh CreateSurface(Transform parent)
        {
            var surfaceObject = new GameObject("Terrain");
            surfaceObject.transform.SetParent(parent, false);

            var meshFilter = surfaceObject.AddComponent<MeshFilter>();
            var meshRenderer = surfaceObject.AddComponent<MeshRenderer>();
            var meshCollider = surfaceObject.AddComponent<MeshCollider>();

            var resolution = mapDefinition.heightMap != null
                ? Mathf.Clamp(
                    mapDefinition.heightResolution,
                    2,
                    MaximumGeneratedSurfaceResolution)
                : 2;
            var mesh = BuildSurfaceMesh(resolution);
            meshFilter.sharedMesh = mesh;
            meshCollider.sharedMesh = mesh;

            var visualMaterial = GetOrCreateVisualMaterial();
            meshRenderer.sharedMaterial = visualMaterial;
            meshRenderer.enabled = visualMaterial != null;
            return mesh;
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
                var materialPath = $"{DefinitionsFolder}/{mapName}_Visual.mat";
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

            mapDefinition.visualMaterial = material;
            EditorUtility.SetDirty(material);
            EditorUtility.SetDirty(mapDefinition);
            AssetDatabase.SaveAssets();
            return material;
        }

        private Mesh BuildSurfaceMesh(int resolution)
        {
            var mesh = new Mesh
            {
                name = $"{mapDefinition.name}_Terrain"
            };

            var vertexCount = resolution * resolution;
            var vertices = new Vector3[vertexCount];
            var uvs = new Vector2[vertexCount];
            var triangles = new int[(resolution - 1) * (resolution - 1) * 6];
            var size = mapDefinition.sourceMax - mapDefinition.sourceMin;
            var vertexIndex = 0;

            for (var z = 0; z < resolution; z++)
            {
                var v = resolution == 1 ? 0f : z / (float)(resolution - 1);
                for (var x = 0; x < resolution; x++)
                {
                    var u = resolution == 1 ? 0f : x / (float)(resolution - 1);
                    var source = new Vector2(
                        Mathf.Lerp(mapDefinition.sourceMin.x, mapDefinition.sourceMax.x, u),
                        Mathf.Lerp(mapDefinition.sourceMin.y, mapDefinition.sourceMax.y, v));
                    var height = SampleHeight(source);

                    vertices[vertexIndex] = new Vector3(
                        source.x * mapDefinition.worldScale,
                        height,
                        source.y * mapDefinition.worldScale);
                    uvs[vertexIndex] = new Vector2(u, v);
                    vertexIndex++;
                }
            }

            var triangleIndex = 0;
            for (var z = 0; z < resolution - 1; z++)
            {
                for (var x = 0; x < resolution - 1; x++)
                {
                    var bottomLeft = z * resolution + x;
                    var topLeft = (z + 1) * resolution + x;
                    var bottomRight = bottomLeft + 1;
                    var topRight = topLeft + 1;

                    triangles[triangleIndex++] = bottomLeft;
                    triangles[triangleIndex++] = topLeft;
                    triangles[triangleIndex++] = topRight;
                    triangles[triangleIndex++] = bottomLeft;
                    triangles[triangleIndex++] = topRight;
                    triangles[triangleIndex++] = bottomRight;
                }
            }

            if (vertexCount > 65535)
            {
                mesh.indexFormat = IndexFormat.UInt32;
            }

            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private float SampleHeight(Vector2 sourcePosition)
        {
            if (mapDefinition.heightMap == null ||
                !mapDefinition.heightMap.isReadable)
            {
                return mapDefinition.baseHeight;
            }

            var u = Mathf.InverseLerp(
                mapDefinition.sourceMin.x,
                mapDefinition.sourceMax.x,
                sourcePosition.x);
            var v = Mathf.InverseLerp(
                mapDefinition.sourceMin.y,
                mapDefinition.sourceMax.y,
                sourcePosition.y);
            if (mapDefinition.flipHeightY)
            {
                v = 1f - v;
            }

            return mapDefinition.baseHeight +
                   mapDefinition.heightMap.GetPixelBilinear(u, v).r *
                   mapDefinition.heightScale;
        }

        private void CreateBuildingWalls(Transform parent)
        {
            if (!mapDefinition.generateBuildingWalls ||
                mapDefinition.wallHeight <= 0f)
            {
                return;
            }

            var wallRoot = new GameObject("BuildingColliders");
            wallRoot.transform.SetParent(parent, false);
            var wallIndex = 0;

            foreach (var ring in analyzedLayout.buildingRings)
            {
                for (var i = 0; i < ring.Count; i++)
                {
                    var start = ring[i];
                    var end = ring[(i + 1) % ring.Count];
                    var edge = end - start;
                    var length = edge.magnitude * mapDefinition.worldScale;
                    if (length <= 0.001f)
                    {
                        continue;
                    }

                    var midpoint = (start + end) * 0.5f;
                    var center = new Vector3(
                        midpoint.x * mapDefinition.worldScale,
                        SampleHeight(midpoint) + mapDefinition.wallHeight * 0.5f,
                        midpoint.y * mapDefinition.worldScale);
                    var wall = new GameObject($"BuildingWall_{wallIndex:0000}");
                    wall.transform.SetParent(wallRoot.transform, false);
                    wall.transform.position = center;
                    wall.transform.rotation = Quaternion.Euler(
                        0f,
                        Mathf.Atan2(edge.y, edge.x) * Mathf.Rad2Deg,
                        0f);

                    var collider = wall.AddComponent<BoxCollider>();
                    collider.size = new Vector3(
                        length,
                        mapDefinition.wallHeight,
                        mapDefinition.wallThickness);
                    wallIndex++;
                }
            }
        }

        private void CreateTreeColliders(Transform parent)
        {
            var treeRoot = new GameObject("TreeColliders");
            treeRoot.transform.SetParent(parent, false);

            for (var i = 0; i < analyzedLayout.treePoints.Count; i++)
            {
                var point = analyzedLayout.treePoints[i];
                var tree = new GameObject($"TreeCollider_{i:0000}");
                tree.transform.SetParent(treeRoot.transform, false);
                tree.transform.position = new Vector3(
                    point.x * mapDefinition.worldScale,
                    SampleHeight(point) + 1f,
                    point.y * mapDefinition.worldScale);

                var collider = tree.AddComponent<CapsuleCollider>();
                collider.radius = 0.5f;
                collider.height = 2f;
            }
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
            heightScale = mapDefinition.heightScale;
            heightResolution = mapDefinition.heightResolution;
            wallHeight = mapDefinition.wallHeight;
            wallThickness = mapDefinition.wallThickness;
            generateBuildingWalls = mapDefinition.generateBuildingWalls;
            generateTreeColliders = mapDefinition.generateTreeColliders;
            replaceGeneratedRoot = mapDefinition.replaceGeneratedRoot;
            AnalyzeLayout();
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

        private static void EnsureDefinitionsFolder()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Maps"))
            {
                AssetDatabase.CreateFolder("Assets", "Maps");
            }

            if (!AssetDatabase.IsValidFolder(DefinitionsFolder))
            {
                AssetDatabase.CreateFolder("Assets/Maps", "Definitions");
            }
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

                    if (id == "values")
                    {
                        data.generator = featureToken.Value<string>("generator") ?? string.Empty;
                        data.generatorVersion = featureToken.Value<string>("version") ?? string.Empty;
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
