using System.Collections.Generic;
using BattleRoyale.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BattleRoyale.UI.EditorTools
{
    [CustomEditor(typeof(UITextTheme))]
    sealed class UITextThemeEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.Space(6f);
            using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
            {
                if (GUILayout.Button("Apply to prefabs and open scenes"))
                {
                    UITextThemeApply.ApplyAll((UITextTheme)target);
                }
            }
        }
    }

    [CustomEditor(typeof(UITextStyle))]
    sealed class UITextStyleEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var themeProperty = serializedObject.FindProperty("theme");
            var styleProperty = serializedObject.FindProperty("styleId");
            var labelProperty = serializedObject.FindProperty("label");
            var colorProperty = serializedObject.FindProperty("applyColor");

            if (!serializedObject.isEditingMultipleObjects && !EditorApplication.isPlayingOrWillChangePlaymode)
            {
                AssignDefaults(themeProperty, labelProperty);
            }

            EditorGUILayout.PropertyField(themeProperty);
            EditorGUILayout.PropertyField(labelProperty);
            EditorGUILayout.PropertyField(colorProperty);
            DrawStylePopup(themeProperty, styleProperty);
            DrawSelectedPreview(themeProperty, styleProperty, colorProperty);

            serializedObject.ApplyModifiedProperties();
            EditorGUILayout.Space(6f);
            if (!GUILayout.Button("Apply"))
            {
                return;
            }

            for (var i = 0; i < targets.Length; i++)
            {
                var style = (UITextStyle)targets[i];
                if (style == null)
                {
                    continue;
                }

                var label = new SerializedObject(style).FindProperty("label").objectReferenceValue;
                if (label != null)
                {
                    Undo.RecordObject(label, "Apply Text Theme");
                }

                style.Apply();
                EditorUtility.SetDirty(style);
            }
        }

        void AssignDefaults(SerializedProperty themeProperty, SerializedProperty labelProperty)
        {
            if (labelProperty.objectReferenceValue == null)
            {
                var found = ((UITextStyle)target).GetComponent<TMP_Text>();
                if (found != null)
                {
                    labelProperty.objectReferenceValue = found;
                }
            }

            if (themeProperty.objectReferenceValue != null)
            {
                return;
            }

            var guids = AssetDatabase.FindAssets("t:UITextTheme");
            if (guids.Length != 1)
            {
                return;
            }

            var path = AssetDatabase.GUIDToAssetPath(guids[0]);
            themeProperty.objectReferenceValue = AssetDatabase.LoadAssetAtPath<UITextTheme>(path);
        }

        static void DrawStylePopup(SerializedProperty themeProperty, SerializedProperty styleProperty)
        {
            var theme = themeProperty.objectReferenceValue as UITextTheme;
            if (themeProperty.hasMultipleDifferentValues)
            {
                EditorGUILayout.PropertyField(styleProperty);
                return;
            }

            if (theme == null)
            {
                EditorGUILayout.HelpBox("Assign a text theme to choose a style.", MessageType.Info);
                return;
            }

            var ids = ReadIds(theme);
            if (ids.Length == 0)
            {
                EditorGUILayout.HelpBox("Add styles to the theme.", MessageType.Warning);
                return;
            }

            var current = styleProperty.hasMultipleDifferentValues ? string.Empty : styleProperty.stringValue;
            var index = IndexOf(ids, current);
            var labels = ids;
            if (index < 0)
            {
                labels = new string[ids.Length + 1];
                labels[0] = string.IsNullOrEmpty(current) ? "(choose a style)" : current + " (missing)";
                for (var i = 0; i < ids.Length; i++)
                {
                    labels[i + 1] = ids[i];
                }

                index = 0;
            }

            EditorGUI.BeginChangeCheck();
            var next = EditorGUILayout.Popup("Style", index, labels);
            if (!EditorGUI.EndChangeCheck())
            {
                return;
            }

            var idIndex = labels.Length == ids.Length ? next : next - 1;
            if (idIndex >= 0 && idIndex < ids.Length)
            {
                styleProperty.stringValue = ids[idIndex];
            }
        }

        static void DrawSelectedPreview(
            SerializedProperty themeProperty,
            SerializedProperty styleProperty,
            SerializedProperty colorProperty)
        {
            if (themeProperty.hasMultipleDifferentValues || styleProperty.hasMultipleDifferentValues)
            {
                return;
            }

            var theme = themeProperty.objectReferenceValue as UITextTheme;
            if (theme == null || !theme.TryGet(styleProperty.stringValue, out var style))
            {
                return;
            }

            var height = Mathf.Clamp(style.fontSize, 12f, 64f) + 12f;
            var rect = GUILayoutUtility.GetRect(0f, height, GUILayout.ExpandWidth(true));
            var font = style.font == null ? null : style.font.sourceFontFile;
            var sample = string.IsNullOrEmpty(style.id) ? "Ag" : style.id;
            UITextStylePreview.Draw(rect, font, Mathf.Clamp(style.fontSize, 12f, 64f), style.color, sample);
            if (!colorProperty.hasMultipleDifferentValues && !colorProperty.boolValue)
            {
                EditorGUILayout.LabelField("This label keeps its own color.", EditorStyles.miniLabel);
            }
        }

        static string[] ReadIds(UITextTheme theme)
        {
            var serialized = new SerializedObject(theme);
            var styles = serialized.FindProperty("styles");
            if (styles == null || !styles.isArray)
            {
                return new string[0];
            }

            var ids = new List<string>(styles.arraySize);
            for (var i = 0; i < styles.arraySize; i++)
            {
                var id = styles.GetArrayElementAtIndex(i).FindPropertyRelative("id").stringValue;
                if (!string.IsNullOrEmpty(id))
                {
                    ids.Add(id);
                }
            }

            return ids.ToArray();
        }

        static int IndexOf(string[] ids, string id)
        {
            var normalized = UITextTheme.NormalizeId(id);
            for (var i = 0; i < ids.Length; i++)
            {
                if (UITextTheme.NormalizeId(ids[i]) == normalized)
                {
                    return i;
                }
            }

            return -1;
        }
    }

    static class UITextThemeApply
    {
        public static void ApplyAll(UITextTheme theme)
        {
            if (theme == null)
            {
                Debug.LogError("UITextThemeApply: text theme is missing.");
                return;
            }

            var openPrefabPath = OpenPrefabPath();
            var prefabs = ApplyPrefabs(theme, openPrefabPath);
            var stage = ApplyOpenPrefabStage(theme);
            var scenes = ApplyOpenScenes(theme);
            Debug.Log(
                $"Applied '{theme.name}' to {prefabs} prefabs, {stage} open prefab stage, and {scenes} scene objects.");
        }

        static string OpenPrefabPath()
        {
            var stage = PrefabStageUtility.GetCurrentPrefabStage();
            return stage == null ? null : stage.assetPath;
        }

        static int ApplyPrefabs(UITextTheme theme, string skipPath)
        {
            var count = 0;
            var guids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" });
            for (var i = 0; i < guids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (path == skipPath)
                {
                    continue;
                }

                var root = PrefabUtility.LoadPrefabContents(path);
                if (root == null)
                {
                    continue;
                }

                var changed = ApplyOwned(root, theme);
                if (changed)
                {
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                    count++;
                }

                PrefabUtility.UnloadPrefabContents(root);
            }

            return count;
        }

        static int ApplyOpenPrefabStage(UITextTheme theme)
        {
            var stage = PrefabStageUtility.GetCurrentPrefabStage();
            if (stage == null || stage.prefabContentsRoot == null)
            {
                return 0;
            }

            if (!ApplyOwned(stage.prefabContentsRoot, theme))
            {
                return 0;
            }

            EditorUtility.SetDirty(stage.prefabContentsRoot);
            return 1;
        }

        static int ApplyOpenScenes(UITextTheme theme)
        {
            var count = 0;
            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded)
                {
                    continue;
                }

                var roots = scene.GetRootGameObjects();
                var sceneCount = 0;
                for (var r = 0; r < roots.Length; r++)
                {
                    var styles = roots[r].GetComponentsInChildren<UITextStyle>(true);
                    for (var s = 0; s < styles.Length; s++)
                    {
                        var style = styles[s];
                        if (PrefabUtility.IsPartOfPrefabInstance(style) || !style.Uses(theme))
                        {
                            continue;
                        }

                        var label = new SerializedObject(style).FindProperty("label").objectReferenceValue;
                        if (label != null)
                        {
                            Undo.RecordObject(label, "Apply Text Theme");
                        }

                        style.Apply();
                        EditorUtility.SetDirty(style);
                        sceneCount++;
                    }
                }

                if (sceneCount > 0)
                {
                    EditorSceneManager.MarkSceneDirty(scene);
                    count += sceneCount;
                }
            }

            return count;
        }

        static bool ApplyOwned(GameObject prefabRoot, UITextTheme theme)
        {
            var styles = prefabRoot.GetComponentsInChildren<UITextStyle>(true);
            var changed = false;
            for (var i = 0; i < styles.Length; i++)
            {
                var style = styles[i];
                if (!Owns(style, prefabRoot) || !style.Uses(theme))
                {
                    continue;
                }

                style.Apply();
                changed = true;
            }

            return changed;
        }

        static bool Owns(Component component, GameObject prefabRoot)
        {
            if (!PrefabUtility.IsPartOfPrefabInstance(component))
            {
                return true;
            }

            var nearest = PrefabUtility.GetNearestPrefabInstanceRoot(component);
            return nearest == null || nearest == prefabRoot;
        }
    }
}
