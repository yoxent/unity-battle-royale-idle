#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace BattleRoyale.Maps.EditorTools
{
    public sealed partial class MapAuthoringWindow
    {
        const string UpperLeftMarkerName = "ImageCornerUpperLeft";
        const string LowerRightMarkerName = "ImageCornerLowerRight";
        const string CornerDiscName = "MarkerDisc";
        const float CornerMarkerDiameter = 12f;
        const string CornerMarkerMaterialPath = "Assets/Materials/MarkerCube.mat";

        static Mesh cornerDiscMesh;

        void DrawImageCornerSection()
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
                    "Keep the full exported image, including the spacing around the drawing. Upper Left and Lower Right mark where the map drawing sits inside that image."),
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
                    imageAlignmentSaveRequested = true;
                    QueueDeferredAction();
                }

                return;
            }

            EditorGUI.BeginChangeCheck();
            imageUpperLeftUv = EditorGUILayout.Vector2Field(
                new GUIContent(
                    "Upper Left",
                    "Image coordinate of the map drawing's top-left corner. X moves across the image. Y is 1 at the top."),
                imageUpperLeftUv);
            imageLowerRightUv = EditorGUILayout.Vector2Field(
                new GUIContent(
                    "Lower Right",
                    "Image coordinate of the map drawing's bottom-right corner. X moves across the image. Y is 0 at the bottom."),
                imageLowerRightUv);
            editImageCornersInScene = EditorGUILayout.Toggle(
                new GUIContent(
                    "Edit Corner Points In Scene",
                    "Drag the two points onto the corners of the drawn map. The full image, including its outer spacing, stays visible."),
                editImageCornersInScene);
            var fieldsChanged = EditorGUI.EndChangeCheck();

            if (!ImageCornerFit.IsValid(imageUpperLeftUv, imageLowerRightUv))
            {
                EditorGUILayout.HelpBox(
                    "Upper Left must be above and to the left of Lower Right on the image.",
                    MessageType.Warning);
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "Drag the Upper Left and Lower Right handles in the Scene view onto the corners of the drawn map, then release. The picture moves to the floor. The discs show those points in the Game view.",
                    MessageType.None);
            }

            if (GUILayout.Button("Apply Corner Points To Scene"))
            {
                fieldsChanged = true;
            }

            if (fieldsChanged || previousFit != useImageCornerFit)
            {
                imageAlignmentSaveRequested = true;
                imageAlignmentApplyRequested = true;
                QueueDeferredAction();
            }

            UpdateCornerSceneGui();
            if (!draggingUpperLeft && !draggingLowerRight)
            {
                SyncImageCornerMarkers();
            }
        }

        Vector3[] VisualCorners()
        {
            if (!useImageCornerFit ||
                !ImageCornerFit.IsValid(imageUpperLeftUv, imageLowerRightUv))
            {
                return SourceCorners();
            }

            var upperLeft = SourceUpperLeft();
            var lowerRight = SourceLowerRight();
            return new[]
            {
                ImageCornerFit.ToLocal(
                    new Vector2(0f, 1f),
                    imageUpperLeftUv,
                    imageLowerRightUv,
                    upperLeft,
                    lowerRight),
                ImageCornerFit.ToLocal(
                    new Vector2(0f, 0f),
                    imageUpperLeftUv,
                    imageLowerRightUv,
                    upperLeft,
                    lowerRight),
                ImageCornerFit.ToLocal(
                    new Vector2(1f, 0f),
                    imageUpperLeftUv,
                    imageLowerRightUv,
                    upperLeft,
                    lowerRight),
                ImageCornerFit.ToLocal(
                    new Vector2(1f, 1f),
                    imageUpperLeftUv,
                    imageLowerRightUv,
                    upperLeft,
                    lowerRight)
            };
        }

        Vector3 SourceUpperLeft()
        {
            var scale = mapDefinition.worldScale;
            return new Vector3(
                mapDefinition.sourceMin.x * scale,
                0f,
                mapDefinition.sourceMax.y * scale);
        }

        Vector3 SourceLowerRight()
        {
            var scale = mapDefinition.worldScale;
            return new Vector3(
                mapDefinition.sourceMax.x * scale,
                0f,
                mapDefinition.sourceMin.y * scale);
        }

        Vector3 ImageUvToWorld(Vector2 imageUv, Transform visual = null)
        {
            var local = ImageCornerFit.ToLocal(
                imageUv,
                imageUpperLeftUv,
                imageLowerRightUv,
                SourceUpperLeft(),
                SourceLowerRight());
            visual ??= FindVisualMap();
            if (visual == null)
            {
                return local + new Vector3(0f, baseHeight - 0.02f, 0f);
            }

            return visual.TransformPoint(local);
        }

        Vector2 WorldToImageUv(Vector3 worldPosition)
        {
            var visual = FindVisualMap();
            var local = visual != null
                ? visual.InverseTransformPoint(worldPosition)
                : worldPosition;
            return ImageCornerFit.ToImageUv(
                local,
                imageUpperLeftUv,
                imageLowerRightUv,
                SourceUpperLeft(),
                SourceLowerRight());
        }

        void CopyImageCornersToDefinition()
        {
            if (mapDefinition == null)
            {
                return;
            }

            EnsureDefaultImageCorners();
            mapDefinition.useImageCornerFit = useImageCornerFit;
            mapDefinition.imageUpperLeftUv = imageUpperLeftUv;
            mapDefinition.imageLowerRightUv = imageLowerRightUv;
            mapDefinition.editImageCornersInScene = editImageCornersInScene;
            EditorUtility.SetDirty(mapDefinition);
            AssetDatabase.SaveAssets();
        }

        void ApplyImageCornerFitToScene()
        {
            if (!useImageCornerFit ||
                !ImageCornerFit.IsValid(imageUpperLeftUv, imageLowerRightUv) ||
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
                    "Generate the village map before applying image corner points.",
                    MessageType.Warning);
                return;
            }

            var meshFilter = visual.GetComponent<MeshFilter>();
            if (meshFilter == null)
            {
                return;
            }

            var previousMesh = meshFilter.sharedMesh;
            meshFilter.sharedMesh = BuildVisualQuad();
            if (previousMesh != null && !AssetDatabase.Contains(previousMesh))
            {
                DestroyImmediate(previousMesh);
            }

            var floor = visual.parent != null
                ? visual.parent.Find("VillageFloor")
                : null;
            var floorCollider = floor != null
                ? floor.GetComponent<BoxCollider>()
                : null;
            if (floorCollider != null)
            {
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
                floorCollider.center = center;
                floorCollider.size = size;
            }

            EditorSceneManager.MarkSceneDirty(visual.gameObject.scene);
            SyncImageCornerMarkers(visual.parent);
            SetStatus(
                "Fit the full image between the upper-left and lower-right corner points.",
                MessageType.Info);
        }

        void EnsureDefaultImageCorners()
        {
            if (ImageCornerFit.IsValid(imageUpperLeftUv, imageLowerRightUv))
            {
                return;
            }

            imageUpperLeftUv = new Vector2(0f, 1f);
            imageLowerRightUv = new Vector2(1f, 0f);
        }

        void UpdateCornerSceneGui()
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

        void StopCornerSceneGui()
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

        void OnCornerSceneGui(SceneView sceneView)
        {
            if (!useImageCornerFit || !editImageCornersInScene)
            {
                return;
            }

            if (mapDefinition == null || !mapDefinition.HasBounds || FindVisualMap() == null)
            {
                Handles.BeginGUI();
                GUILayout.BeginArea(new Rect(12f, 12f, 360f, 48f));
                EditorGUILayout.HelpBox(
                    "Generate this village map to place the image corner points.",
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

        void DrawImageCornerHandle(
            string label,
            string markerName,
            Color color,
            ref bool dragging,
            ref Vector3 draggedPosition,
            Vector2 currentUv,
            System.Action<Vector3> applyDrag)
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

        void ApplyDraggedUpperLeft(Vector3 worldPosition)
        {
            TryApplyDraggedCorner(worldPosition, true);
        }

        void ApplyDraggedLowerRight(Vector3 worldPosition)
        {
            TryApplyDraggedCorner(worldPosition, false);
        }

        void TryApplyDraggedCorner(Vector3 worldPosition, bool upperLeft)
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

            if (ImageCornerFit.IsValid(imageUpperLeftUv, imageLowerRightUv))
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

        void FinishImageCornerDrag()
        {
            CopyImageCornersToDefinition();
            ApplyImageCornerFitToScene();
            Repaint();
            SceneView.RepaintAll();
        }

        void SyncImageCornerMarkers(Transform explicitRoot = null)
        {
            if (!useImageCornerFit ||
                !ImageCornerFit.IsValid(imageUpperLeftUv, imageLowerRightUv) ||
                mapDefinition == null ||
                !mapDefinition.HasBounds)
            {
                return;
            }

            var root = explicitRoot != null ? explicitRoot : FindGeneratedMapRoot();
            if (root == null)
            {
                return;
            }

            var visual = root.Find("VisualMap");
            var changed = PlaceImageCornerMarker(
                root,
                UpperLeftMarkerName,
                ImageUvToWorld(imageUpperLeftUv, visual),
                new Color(1f, 0.85f, 0.2f));
            changed |= PlaceImageCornerMarker(
                root,
                LowerRightMarkerName,
                ImageUvToWorld(imageLowerRightUv, visual),
                new Color(0.3f, 0.75f, 1f));
            if (!changed)
            {
                return;
            }

            EditorSceneManager.MarkSceneDirty(root.gameObject.scene);
        }

        bool PlaceImageCornerMarker(
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

        void RemoveImageCornerMarkers()
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

        static bool RemoveCornerMarker(Transform root, string markerName)
        {
            var marker = root.Find(markerName);
            if (marker == null)
            {
                return false;
            }

            Undo.DestroyObjectImmediate(marker.gameObject);
            return true;
        }

        static void AddCornerDisc(Transform marker, Color color)
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

        static Mesh GetCornerDiscMesh()
        {
            if (cornerDiscMesh != null)
            {
                return cornerDiscMesh;
            }

            var temp = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cornerDiscMesh = temp.GetComponent<MeshFilter>().sharedMesh;
            Object.DestroyImmediate(temp);
            return cornerDiscMesh;
        }

        Transform FindGeneratedMapRoot()
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

            var maps = Object.FindObjectsByType<MapInstance>(
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

            return null;
        }

        Transform FindVisualMap()
        {
            var root = FindGeneratedMapRoot();
            return root == null ? null : root.Find("VisualMap");
        }
    }
}
#endif
