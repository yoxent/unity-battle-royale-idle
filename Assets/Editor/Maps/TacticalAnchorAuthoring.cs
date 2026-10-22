using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Rendering;

namespace BattleRoyale.Maps.EditorTools
{
    public static class TacticalAnchorAuthoring
    {
        public const string RootName = "Tactical Anchors";

        const string DiscName = "MarkerDisc";
        const string MarkerMaterialPath = "Assets/Materials/MarkerCube.mat";
        const string ColorPref = "BattleRoyale.TacticalAnchor.MarkerColor";
        const string RetiredDefaultHtml = "#FF731A";
        const float DiscDiameter = 12f;
        static readonly Color DefaultMarkerColor = MapTacticalAnchorMarker.DefaultColor;
        static Color markerColor;
        static bool colorLoaded;
        static Mesh discMesh;
        static MapDefinition anchorListMap;
        static ReorderableList anchorList;
        static MapDefinition sceneMap;
        static bool sceneGuiHooked;

        public static Color MarkerColor
        {
            get
            {
                if (!colorLoaded)
                {
                    markerColor = LoadMarkerColor();
                    colorLoaded = true;
                }

                return markerColor;
            }
        }

        public static void SetMarkerColor(Color color)
        {
            color.a = 1f;
            markerColor = color;
            colorLoaded = true;
            EditorPrefs.SetString(ColorPref, "#" + ColorUtility.ToHtmlStringRGB(color));
        }

        public static void Draw(MapDefinition mapDefinition)
        {
            var color = MarkerColor;
            var open = AuthoringSectionHeader.Draw("Tactical Anchors", ref color);
            if (color != MarkerColor)
            {
                SetMarkerColor(color);
                SceneView.RepaintAll();
            }

            sceneMap = mapDefinition;
            EnsureSceneGui();
            SyncScene(mapDefinition);
            if (mapDefinition == null || !open)
            {
                return;
            }

            DrawAnchorList(mapDefinition);

            if (GUILayout.Button("Save Anchor Positions To Map Data"))
            {
                Save(mapDefinition);
            }
        }

        static void DrawAnchorList(MapDefinition mapDefinition)
        {
            mapDefinition.tacticalAnchors ??= new List<MapTacticalAnchor>();
            if (anchorList == null ||
                anchorListMap != mapDefinition ||
                anchorList.list != mapDefinition.tacticalAnchors)
            {
                anchorListMap = mapDefinition;
                anchorList = new ReorderableList(
                    mapDefinition.tacticalAnchors,
                    typeof(MapTacticalAnchor),
                    true,
                    false,
                    true,
                    true)
                {
                    elementHeight = EditorGUIUtility.singleLineHeight + 6f
                };
                anchorList.drawElementCallback = (rect, index, active, focused) =>
                    DrawAnchorElement(mapDefinition, rect, index);
                anchorList.onAddCallback = _ => AddAnchor(mapDefinition);
                anchorList.onRemoveCallback = list => RemoveAnchor(mapDefinition, list.index);
                anchorList.onReorderCallback = _ =>
                {
                    EditorUtility.SetDirty(mapDefinition);
                    SyncScene(mapDefinition);
                    SceneView.RepaintAll();
                };
            }

            if (Event.current.type == EventType.MouseDown)
            {
                Undo.RecordObject(mapDefinition, "Edit Tactical Anchors");
            }

            anchorList.DoLayoutList();
        }

        static void DrawAnchorElement(MapDefinition mapDefinition, Rect rect, int index)
        {
            var anchors = mapDefinition.tacticalAnchors;
            if (anchors == null || index < 0 || index >= anchors.Count)
            {
                return;
            }

            var anchor = anchors[index];
            if (anchor == null)
            {
                anchor = new MapTacticalAnchor();
                anchors[index] = anchor;
            }

            rect.y += 2f;
            rect.height = EditorGUIUtility.singleLineHeight;
            const float weightWidth = 72f;
            var idRect = new Rect(rect.x, rect.y, rect.width - weightWidth - 6f, rect.height);
            var weightRect = new Rect(rect.xMax - weightWidth, rect.y, weightWidth, rect.height);
            EditorGUI.BeginChangeCheck();
            var id = EditorGUI.TextField(idRect, anchor.id ?? string.Empty);
            var weight = EditorGUI.FloatField(weightRect, anchor.weight);
            if (!EditorGUI.EndChangeCheck())
            {
                return;
            }

            Undo.RecordObject(mapDefinition, "Edit Tactical Anchor");
            anchor.id = id;
            anchor.weight = Mathf.Max(0f, weight);
            EditorUtility.SetDirty(mapDefinition);
            SyncScene(mapDefinition);
        }

        static void AddAnchor(MapDefinition mapDefinition)
        {
            mapDefinition.tacticalAnchors ??= new List<MapTacticalAnchor>();
            Undo.RecordObject(mapDefinition, "Add Tactical Anchor");
            mapDefinition.tacticalAnchors.Add(
                new MapTacticalAnchor
                {
                    id = "Anchor " + (mapDefinition.tacticalAnchors.Count + 1),
                    position = mapDefinition.WorldCenter,
                    weight = 1f
                });
            EditorUtility.SetDirty(mapDefinition);
            SyncScene(mapDefinition);
        }

        static void RemoveAnchor(MapDefinition mapDefinition, int index)
        {
            var anchors = mapDefinition.tacticalAnchors;
            if (anchors == null || index < 0 || index >= anchors.Count)
            {
                return;
            }

            Undo.RecordObject(mapDefinition, "Remove Tactical Anchor");
            anchors.RemoveAt(index);
            EditorUtility.SetDirty(mapDefinition);
            SyncScene(mapDefinition);
        }

        public static void SyncScene(MapDefinition mapDefinition)
        {
            Sync(FindMapRoot(mapDefinition), mapDefinition);
        }

        public static void Show(MapDefinition mapDefinition)
        {
            SyncScene(mapDefinition);
        }

        public static void Sync(Transform mapRoot, MapDefinition mapDefinition)
        {
            if (mapRoot == null || mapDefinition == null)
            {
                return;
            }

            mapDefinition.tacticalAnchors ??= new List<MapTacticalAnchor>();
            var root = KeepOneRoot(mapRoot);

            var anchors = mapDefinition.tacticalAnchors;
            for (var index = 0; index < anchors.Count; index++)
            {
                var anchor = anchors[index];
                if (anchor == null)
                {
                    continue;
                }

                var markerTransform = index < root.childCount ? root.GetChild(index) : null;
                var created = markerTransform == null;
                if (created)
                {
                    var markerObject = new GameObject(Label(anchor));
                    Undo.RegisterCreatedObjectUndo(markerObject, "Show Tactical Anchors");
                    markerObject.transform.SetParent(root, false);
                    markerTransform = markerObject.transform;
                }

                var marker = markerTransform.GetComponent<MapTacticalAnchorMarker>();
                if (marker == null)
                {
                    marker = markerTransform.gameObject.AddComponent<MapTacticalAnchorMarker>();
                }

                var markerMoved = (markerTransform.localPosition - anchor.position).sqrMagnitude > 0.000001f;
                if (created || marker.anchorId != anchor.id)
                {
                    if (created || markerMoved)
                    {
                        SetLocalPosition(markerTransform, anchor.position);
                    }
                }
                else if (markerMoved)
                {
                    anchor.position = markerTransform.localPosition;
                    EditorUtility.SetDirty(mapDefinition);
                }

                markerTransform.name = Label(anchor);

                marker.anchorId = anchor.id;
                marker.weight = anchor.weight;
                marker.index = index;
                marker.color = MarkerColor;
                AddDisc(markerTransform);
            }

            for (var index = root.childCount - 1; index >= anchors.Count; index--)
            {
                Undo.DestroyObjectImmediate(root.GetChild(index).gameObject);
            }
        }

        public static void StopSceneGui()
        {
            if (!sceneGuiHooked)
            {
                return;
            }

            SceneView.duringSceneGui -= OnSceneGUI;
            sceneGuiHooked = false;
            sceneMap = null;
        }

        static void EnsureSceneGui()
        {
            if (sceneGuiHooked)
            {
                return;
            }

            SceneView.duringSceneGui += OnSceneGUI;
            sceneGuiHooked = true;
        }

        static void OnSceneGUI(SceneView sceneView)
        {
            if (sceneMap == null || !sceneMap.editPointsInSceneView)
            {
                return;
            }

            var mapRoot = FindMapRoot(sceneMap);
            if (mapRoot == null)
            {
                return;
            }

            DrawHandles(mapRoot, sceneMap);
        }

        public static void DrawHandles(Transform mapRoot, MapDefinition mapDefinition)
        {
            if (mapRoot == null ||
                mapDefinition == null ||
                mapDefinition.tacticalAnchors == null)
            {
                return;
            }

            Sync(mapRoot, mapDefinition);
            var anchors = mapDefinition.tacticalAnchors;
            for (var index = 0; index < anchors.Count; index++)
            {
                var anchor = anchors[index];
                if (anchor == null)
                {
                    continue;
                }

                var worldPosition = mapRoot.TransformPoint(anchor.position);
                Handles.color = MarkerColor;
                Handles.SphereHandleCap(
                    0,
                    worldPosition,
                    Quaternion.identity,
                    HandleUtility.GetHandleSize(worldPosition) * 0.2f,
                    EventType.Repaint);
                DrawColoredLabel(worldPosition + Vector3.up * 2f, Label(anchor), MarkerColor);

                EditorGUI.BeginChangeCheck();
                var moved = Handles.PositionHandle(worldPosition, Quaternion.identity);
                if (!EditorGUI.EndChangeCheck())
                {
                    continue;
                }

                Undo.RecordObject(mapDefinition, "Move Tactical Anchor");
                anchor.position = mapRoot.InverseTransformPoint(moved);
                EditorUtility.SetDirty(mapDefinition);
                var root = mapRoot.Find(RootName);
                if (root != null && index < root.childCount)
                {
                    SetLocalPosition(root.GetChild(index), anchor.position);
                }
            }
        }

        public static void Save(MapDefinition mapDefinition)
        {
            var mapRoot = FindMapRoot(mapDefinition);
            if (mapRoot == null || mapDefinition == null)
            {
                return;
            }

            var root = mapRoot.Find(RootName);
            if (root == null)
            {
                return;
            }

            var markers = root.GetComponentsInChildren<MapTacticalAnchorMarker>(true);
            Undo.RecordObject(mapDefinition, "Save Anchor Positions To Map Data");
            mapDefinition.tacticalAnchors = new List<MapTacticalAnchor>(markers.Length);
            for (var index = 0; index < markers.Length; index++)
            {
                var marker = markers[index];
                mapDefinition.tacticalAnchors.Add(
                    new MapTacticalAnchor
                    {
                        id = marker.anchorId,
                        position = marker.transform.localPosition,
                        weight = Mathf.Max(0f, marker.weight)
                    });
            }

            EditorUtility.SetDirty(mapDefinition);
        }

        static Transform KeepOneRoot(Transform mapRoot)
        {
            Transform kept = null;
            var extras = new List<GameObject>();
            for (var index = 0; index < mapRoot.childCount; index++)
            {
                var child = mapRoot.GetChild(index);
                if (child.name != RootName)
                {
                    continue;
                }

                if (kept == null)
                {
                    kept = child;
                    continue;
                }

                extras.Add(child.gameObject);
            }

            for (var index = 0; index < extras.Count; index++)
            {
                Undo.DestroyObjectImmediate(extras[index]);
            }

            if (kept != null)
            {
                return kept;
            }

            var rootObject = new GameObject(RootName);
            Undo.RegisterCreatedObjectUndo(rootObject, "Show Tactical Anchors");
            rootObject.transform.SetParent(mapRoot, false);
            return rootObject.transform;
        }

        static void SetLocalPosition(Transform markerTransform, Vector3 localPosition)
        {
            if ((markerTransform.localPosition - localPosition).sqrMagnitude <= 0.000001f)
            {
                return;
            }

            markerTransform.localPosition = localPosition;
            if (PrefabUtility.IsPartOfPrefabInstance(markerTransform))
            {
                PrefabUtility.RecordPrefabInstancePropertyModifications(markerTransform);
            }
        }

        static string Label(MapTacticalAnchor anchor)
        {
            return string.IsNullOrWhiteSpace(anchor.id) ? "Anchor" : anchor.id;
        }

        static void DrawColoredLabel(Vector3 worldPosition, string text, Color color)
        {
            AuthoringMarkerColor.DrawLabel(worldPosition, text, color);
        }

        static Color LoadMarkerColor()
        {
            var html = EditorPrefs.GetString(ColorPref, string.Empty);
            if (string.Equals(html, RetiredDefaultHtml, System.StringComparison.OrdinalIgnoreCase))
            {
                EditorPrefs.DeleteKey(ColorPref);
                html = string.Empty;
            }

            if (ColorUtility.TryParseHtmlString(html, out var color))
            {
                color.a = 1f;
                return color;
            }

            return DefaultMarkerColor;
        }

        static void AddDisc(Transform marker)
        {
            var disc = marker.Find(DiscName);
            if (disc == null)
            {
                var discObject = new GameObject(DiscName);
                discObject.transform.SetParent(marker, false);
                discObject.transform.localPosition = new Vector3(0f, 0.25f, 0f);
                discObject.transform.localScale = new Vector3(DiscDiameter, 0.04f, DiscDiameter);
                var meshFilter = discObject.AddComponent<MeshFilter>();
                meshFilter.sharedMesh = GetDiscMesh();
                var renderer = discObject.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(MarkerMaterialPath);
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                disc = discObject.transform;
            }

            var discRenderer = disc.GetComponent<MeshRenderer>();
            if (discRenderer == null)
            {
                return;
            }

            var block = new MaterialPropertyBlock();
            discRenderer.GetPropertyBlock(block);
            block.SetColor("_BaseColor", MarkerColor);
            block.SetColor("_Color", MarkerColor);
            discRenderer.SetPropertyBlock(block);
        }

        static Mesh GetDiscMesh()
        {
            if (discMesh != null)
            {
                return discMesh;
            }

            var temp = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            discMesh = temp.GetComponent<MeshFilter>().sharedMesh;
            Object.DestroyImmediate(temp);
            return discMesh;
        }

        static Transform FindMapRoot(MapDefinition mapDefinition)
        {
            if (mapDefinition == null)
            {
                return null;
            }

            var maps = Resources.FindObjectsOfTypeAll<MapInstance>();
            for (var index = 0; index < maps.Length; index++)
            {
                var mapInstance = maps[index];
                if (mapInstance.definition == mapDefinition &&
                    mapInstance.gameObject.scene.IsValid() &&
                    mapInstance.gameObject.scene.isLoaded)
                {
                    return mapInstance.transform;
                }
            }

            return null;
        }
    }
}
