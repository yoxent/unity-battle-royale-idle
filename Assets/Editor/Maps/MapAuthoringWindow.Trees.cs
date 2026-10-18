#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BattleRoyale.Maps.EditorTools
{
    public sealed partial class MapAuthoringWindow
    {
        const string TreeUpperLeftMarkerName = "TreeCornerUpperLeft";
        const string TreeLowerRightMarkerName = "TreeCornerLowerRight";

        void DrawTreeCornerOptions()
        {
            if (analyzedLayout == null)
            {
                AnalyzeLayout();
            }

            EnsureDefaultTreeCorners();
            EditorGUI.BeginChangeCheck();
            treeRadiusMultiplier = Mathf.Max(
                0.01f,
                EditorGUILayout.FloatField(
                    new GUIContent(
                        "Radius Multiplier",
                        "Scales the tree collider and the cylinder around the trunk. 1 keeps the base radius of 0.5."),
                    treeRadiusMultiplier));
            treeHeight = Mathf.Max(
                0.01f,
                EditorGUILayout.FloatField(
                    new GUIContent(
                        "Tree Height",
                        "Height of the tree mesh and collider. The bottom stays on the floor. 2 is the default cylinder."),
                    treeHeight));
            treeUpperLeft = EditorGUILayout.Vector2Field(
                new GUIContent(
                    "Upper Left",
                    "Source coordinate of the top-left of the tree area. X moves across the map. Y grows upward. Negative values are allowed."),
                treeUpperLeft);
            treeLowerRight = EditorGUILayout.Vector2Field(
                new GUIContent(
                    "Lower Right",
                    "Source coordinate of the bottom-right of the tree area. X moves across the map. Y grows upward. Negative values are allowed."),
                treeLowerRight);
            editTreeCornersInScene = EditorGUILayout.Toggle(
                new GUIContent(
                    "Edit Corner Points In Scene",
                    "Drag the two points to move the whole tree area. Each tree keeps its place inside that rectangle."),
                editTreeCornersInScene);
            var fieldsChanged = EditorGUI.EndChangeCheck();

            if (!ImageCornerFit.IsValid(treeUpperLeft, treeLowerRight))
            {
                EditorGUILayout.HelpBox(
                    "Upper Left must be above and to the left of Lower Right.",
                    MessageType.Warning);
            }
            else if (!TryGetTreeSourceBounds(out _, out _))
            {
                EditorGUILayout.HelpBox(
                    "Analyze the JSON before placing trees.",
                    MessageType.None);
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "Drag the Upper Left and Lower Right handles in the Scene view to move the tree area, then release. The trees move with that rectangle.",
                    MessageType.None);
            }

            if (GUILayout.Button("Apply Tree Points To Scene"))
            {
                fieldsChanged = true;
            }

            if (fieldsChanged)
            {
                treeSaveRequested = true;
                treeApplyRequested = true;
                QueueDeferredAction();
            }

            UpdateTreeSceneGui();
            if (!draggingTreeUpperLeft && !draggingTreeLowerRight)
            {
                SyncTreeCornerMarkers();
            }
        }

        Vector2 PlacedTreePoint(Vector2 point)
        {
            if (!TryGetTreeSourceBounds(out var sourceMin, out var sourceMax) ||
                !ImageCornerFit.IsValid(treeUpperLeft, treeLowerRight))
            {
                return point;
            }

            return TreePointPlacement.ToSource(
                point,
                sourceMin,
                sourceMax,
                treeUpperLeft,
                treeLowerRight);
        }

        Vector3 TreeSourceToWorld(Vector2 source)
        {
            var scale = mapDefinition != null ? mapDefinition.worldScale : worldScale;
            var height = mapDefinition != null ? mapDefinition.baseHeight : baseHeight;
            var local = new Vector3(source.x * scale, height + 1f, source.y * scale);
            var root = FindGeneratedMapRoot();
            return root != null ? root.TransformPoint(local) : local;
        }

        Vector2 WorldToTreeSource(Vector3 worldPosition)
        {
            var scale = mapDefinition != null ? mapDefinition.worldScale : worldScale;
            if (Mathf.Abs(scale) <= 0.0001f)
            {
                scale = 1f;
            }

            var root = FindGeneratedMapRoot();
            var local = root != null
                ? root.InverseTransformPoint(worldPosition)
                : worldPosition;
            return new Vector2(local.x / scale, local.z / scale);
        }

        void EnsureDefaultTreeCorners()
        {
            if (ImageCornerFit.IsValid(treeUpperLeft, treeLowerRight) ||
                !TryGetTreeSourceBounds(out var sourceMin, out var sourceMax))
            {
                return;
            }

            treeUpperLeft = new Vector2(sourceMin.x, sourceMax.y);
            treeLowerRight = new Vector2(sourceMax.x, sourceMin.y);
        }

        float TreeRadiusScale()
        {
            return Mathf.Max(0.01f, treeRadiusMultiplier <= 0f ? 1f : treeRadiusMultiplier);
        }

        float TreeHeight()
        {
            return Mathf.Max(0.01f, treeHeight <= 0f ? 2f : treeHeight);
        }

        bool TryGetTreeSourceBounds(out Vector2 sourceMin, out Vector2 sourceMax)
        {
            sourceMin = default;
            sourceMax = default;
            if (analyzedLayout == null || analyzedLayout.treePoints.Count == 0)
            {
                return false;
            }

            var minX = float.PositiveInfinity;
            var minY = float.PositiveInfinity;
            var maxX = float.NegativeInfinity;
            var maxY = float.NegativeInfinity;
            foreach (var point in analyzedLayout.treePoints)
            {
                if (point.x < minX)
                {
                    minX = point.x;
                }

                if (point.y < minY)
                {
                    minY = point.y;
                }

                if (point.x > maxX)
                {
                    maxX = point.x;
                }

                if (point.y > maxY)
                {
                    maxY = point.y;
                }
            }

            if (maxX - minX <= 0.0001f || maxY - minY <= 0.0001f)
            {
                return false;
            }

            sourceMin = new Vector2(minX, minY);
            sourceMax = new Vector2(maxX, maxY);
            return true;
        }

        void CopyTreeCornersToDefinition()
        {
            if (mapDefinition == null)
            {
                return;
            }

            EnsureDefaultTreeCorners();
            mapDefinition.generateTreeColliders = generateTreeColliders;
            mapDefinition.treeUpperLeft = treeUpperLeft;
            mapDefinition.treeLowerRight = treeLowerRight;
            mapDefinition.editTreeCornersInScene = editTreeCornersInScene;
            mapDefinition.treeRadiusMultiplier = TreeRadiusScale();
            mapDefinition.treeHeight = TreeHeight();
            EditorUtility.SetDirty(mapDefinition);
            AssetDatabase.SaveAssets();
        }

        void ApplyTreePointsToScene()
        {
            if (!generateTreeColliders)
            {
                return;
            }

            var root = FindGeneratedMapRoot();
            var trees = root != null ? root.Find("Trees") : null;
            if (trees == null)
            {
                SetStatus(
                    "Generate the village map with trees before applying tree points.",
                    MessageType.Warning);
                return;
            }

            var radiusScale = TreeRadiusScale();
            var canPlace = analyzedLayout != null &&
                           ImageCornerFit.IsValid(treeUpperLeft, treeLowerRight);
            var mesh = BuildTreeMesh();
            if (mesh == null)
            {
                SetStatus("The tree points did not produce a mesh.", MessageType.Warning);
                return;
            }

            while (trees.childCount > 0)
            {
                DestroyImmediate(trees.GetChild(0).gameObject);
            }

            var capsule = trees.GetComponent<CapsuleCollider>();
            if (capsule != null)
            {
                DestroyImmediate(capsule);
            }

            var filter = trees.GetComponent<MeshFilter>();
            if (filter == null)
            {
                filter = trees.gameObject.AddComponent<MeshFilter>();
            }

            var previous = filter.sharedMesh;
            filter.sharedMesh = mesh;
            var collider = trees.GetComponent<MeshCollider>();
            if (collider == null)
            {
                collider = trees.gameObject.AddComponent<MeshCollider>();
            }

            collider.sharedMesh = mesh;
            collider.convex = false;
            var renderer = trees.GetComponent<MeshRenderer>();
            if (renderer == null)
            {
                renderer = trees.gameObject.AddComponent<MeshRenderer>();
            }

            renderer.sharedMaterial = WallBoxMesh.SharedMaterial();
            if (previous != null &&
                previous != mesh &&
                previous != sharedTreeMesh &&
                !AssetDatabase.Contains(previous))
            {
                DestroyImmediate(previous);
            }

            EditorSceneManager.MarkSceneDirty(trees.gameObject.scene);
            SyncTreeCornerMarkers();
            SetStatus(
                canPlace
                    ? $"Set the tree radius multiplier to {radiusScale:0.##} and moved the trees into the corner points."
                    : $"Set the tree radius multiplier to {radiusScale:0.##}.",
                MessageType.Info);
        }

        void UpdateTreeSceneGui()
        {
            var shouldRegister = editTreeCornersInScene && generateTreeColliders;
            if (shouldRegister == treeSceneGuiRegistered)
            {
                return;
            }

            if (shouldRegister)
            {
                SceneView.duringSceneGui += OnTreeSceneGui;
                treeSceneGuiRegistered = true;
            }
            else
            {
                StopTreeSceneGui();
            }

            SceneView.RepaintAll();
        }

        void StopTreeSceneGui()
        {
            if (!treeSceneGuiRegistered)
            {
                return;
            }

            SceneView.duringSceneGui -= OnTreeSceneGui;
            treeSceneGuiRegistered = false;
            draggingTreeUpperLeft = false;
            draggingTreeLowerRight = false;
        }

        void OnTreeSceneGui(SceneView sceneView)
        {
            if (!generateTreeColliders || !editTreeCornersInScene)
            {
                return;
            }

            if (!TryGetTreeSourceBounds(out _, out _) || FindGeneratedMapRoot() == null)
            {
                Handles.BeginGUI();
                GUILayout.BeginArea(new Rect(12f, 64f, 360f, 48f));
                EditorGUILayout.HelpBox(
                    "Generate this village map with trees to place the tree corner points.",
                    MessageType.Info);
                GUILayout.EndArea();
                Handles.EndGUI();
                return;
            }

            DrawTreeCornerHandle(
                "Trees Upper Left",
                TreeUpperLeftMarkerName,
                new Color(0.4f, 0.9f, 0.35f),
                ref draggingTreeUpperLeft,
                ref draggedTreeUpperLeft,
                treeUpperLeft,
                ApplyDraggedTreeUpperLeft);
            DrawTreeCornerHandle(
                "Trees Lower Right",
                TreeLowerRightMarkerName,
                new Color(0.95f, 0.45f, 0.2f),
                ref draggingTreeLowerRight,
                ref draggedTreeLowerRight,
                treeLowerRight,
                ApplyDraggedTreeLowerRight);
        }

        void DrawTreeCornerHandle(
            string label,
            string markerName,
            Color color,
            ref bool dragging,
            ref Vector3 draggedPosition,
            Vector2 current,
            System.Action<Vector3> applyDrag)
        {
            var position = dragging
                ? draggedPosition
                : TreeSourceToWorld(current);
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

        void ApplyDraggedTreeUpperLeft(Vector3 worldPosition)
        {
            TryApplyDraggedTreeCorner(worldPosition, true);
        }

        void ApplyDraggedTreeLowerRight(Vector3 worldPosition)
        {
            TryApplyDraggedTreeCorner(worldPosition, false);
        }

        void TryApplyDraggedTreeCorner(Vector3 worldPosition, bool upperLeft)
        {
            var updated = WorldToTreeSource(worldPosition);
            var previousUpperLeft = treeUpperLeft;
            var previousLowerRight = treeLowerRight;
            if (upperLeft)
            {
                treeUpperLeft = updated;
            }
            else
            {
                treeLowerRight = updated;
            }

            if (ImageCornerFit.IsValid(treeUpperLeft, treeLowerRight))
            {
                CopyTreeCornersToDefinition();
                ApplyTreePointsToScene();
                Repaint();
                SceneView.RepaintAll();
                return;
            }

            treeUpperLeft = previousUpperLeft;
            treeLowerRight = previousLowerRight;
            SyncTreeCornerMarkers();
            SetStatus(
                "Upper Left must stay above and to the left of Lower Right.",
                MessageType.Warning);
            Repaint();
        }

        void SyncTreeCornerMarkers()
        {
            if (!generateTreeColliders ||
                !ImageCornerFit.IsValid(treeUpperLeft, treeLowerRight))
            {
                return;
            }

            var root = FindGeneratedMapRoot();
            if (root == null)
            {
                return;
            }

            var changed = PlaceImageCornerMarker(
                root,
                TreeUpperLeftMarkerName,
                TreeSourceToWorld(treeUpperLeft),
                new Color(0.4f, 0.9f, 0.35f));
            changed |= PlaceImageCornerMarker(
                root,
                TreeLowerRightMarkerName,
                TreeSourceToWorld(treeLowerRight),
                new Color(0.95f, 0.45f, 0.2f));
            if (!changed)
            {
                return;
            }

            EditorSceneManager.MarkSceneDirty(root.gameObject.scene);
        }

        void RemoveTreeCornerMarkers()
        {
            var root = FindGeneratedMapRoot();
            if (root == null)
            {
                return;
            }

            var removed = RemoveCornerMarker(root, TreeUpperLeftMarkerName);
            removed |= RemoveCornerMarker(root, TreeLowerRightMarkerName);
            if (removed)
            {
                EditorSceneManager.MarkSceneDirty(root.gameObject.scene);
            }
        }
    }
}
#endif
