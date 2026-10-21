using BattleRoyale.CompanyIdentity;
using UnityEditor;
using UnityEngine;

namespace BattleRoyale.UI.EditorTools
{
    static class CompanyIdentityPrefabWiringMigrator
    {
        const string IdentityPrefabPath = "Assets/UI/Identity/CompanyIdentity.prefab";
        const string EditorPrefabPath = "Assets/UI/Identity/CompanyIdentityEditorView.prefab";

        [MenuItem("Tools/Battle Royale/Integrate Existing Identity Editor")]
        static void IntegrateExistingIdentityEditor()
        {
            var editorPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(EditorPrefabPath);
            if (editorPrefab == null)
            {
                Debug.LogError(
                    $"Identity editor prefab is missing at '{EditorPrefabPath}'.");
                return;
            }

            var root = PrefabUtility.LoadPrefabContents(IdentityPrefabPath);
            if (root == null)
            {
                Debug.LogError(
                    $"Identity prefab could not be loaded at '{IdentityPrefabPath}'.");
                return;
            }

            GameObject newEditor = null;
            try
            {
                var controller = root.GetComponent<CompanyIdentityFlowController>();
                var existingBindings =
                    root.GetComponentInChildren<CompanyIdentityEditorViewBindings>(true);
                if (controller == null || existingBindings == null)
                {
                    Debug.LogError(
                        "Identity prefab needs both a flow controller and an editor binding.");
                    return;
                }

                var existingSource = PrefabUtility.GetCorrespondingObjectFromSource(
                    existingBindings.gameObject);
                if (existingSource == editorPrefab)
                {
                    Debug.Log("The existing identity editor prefab is already integrated.");
                    return;
                }

                var existingRoot = existingBindings.gameObject;
                var parent = existingRoot.transform.parent;
                if (parent == null)
                {
                    Debug.LogError("The identity editor binding has no serialized parent.");
                    return;
                }

                var existingRect = existingRoot.GetComponent<RectTransform>();
                newEditor = PrefabUtility.InstantiatePrefab(editorPrefab, parent) as GameObject;
                if (newEditor == null)
                {
                    Debug.LogError("The existing identity editor prefab could not be instantiated.");
                    return;
                }

                newEditor.name = existingRoot.name;
                CopyRectTransform(existingRect, newEditor.GetComponent<RectTransform>());
                newEditor.SetActive(existingRoot.activeSelf);

                var newBindings = newEditor.GetComponent<CompanyIdentityEditorViewBindings>();
                var newPreviewBindings =
                    newEditor.GetComponentInChildren<CompanyIdentityPreviewViewBindings>(true);
                if (newBindings == null || newPreviewBindings == null)
                {
                    Debug.LogError(
                        "The existing identity editor prefab does not contain its required bindings.");
                    return;
                }

                var serializedController = new SerializedObject(controller);
                SetReference(
                    serializedController,
                    "_editorViewBindings",
                    newBindings);
                SetReference(
                    serializedController,
                    "_editorPreviewBindings",
                    newPreviewBindings);
                serializedController.ApplyModifiedPropertiesWithoutUndo();

                Object.DestroyImmediate(existingRoot);
                PrefabUtility.SaveAsPrefabAsset(root, IdentityPrefabPath);
                AssetDatabase.SaveAssets();
                Debug.Log(
                    "Integrated the existing CompanyIdentityEditorView prefab into CompanyIdentity.");
            }
            finally
            {
                if (newEditor != null &&
                    root.GetComponentInChildren<CompanyIdentityEditorViewBindings>(true) == null)
                {
                    Object.DestroyImmediate(newEditor);
                }

                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        static void SetReference(
            SerializedObject serializedObject,
            string propertyName,
            Object value)
        {
            var property = serializedObject.FindProperty(propertyName);
            if (property == null)
            {
                throw new SerializedPropertyNotFoundException(propertyName);
            }

            property.objectReferenceValue = value;
        }

        static void CopyRectTransform(RectTransform source, RectTransform destination)
        {
            if (source == null || destination == null)
            {
                return;
            }

            destination.anchorMin = source.anchorMin;
            destination.anchorMax = source.anchorMax;
            destination.anchoredPosition = source.anchoredPosition;
            destination.sizeDelta = source.sizeDelta;
            destination.pivot = source.pivot;
            destination.localRotation = source.localRotation;
            destination.localScale = source.localScale;
        }

        sealed class SerializedPropertyNotFoundException : System.Exception
        {
            public SerializedPropertyNotFoundException(string propertyName)
                : base($"Serialized field '{propertyName}' was not found.")
            {
            }
        }
    }
}
