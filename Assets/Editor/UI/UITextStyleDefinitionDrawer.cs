using TMPro;
using UnityEditor;
using UnityEngine;

namespace BattleRoyale.UI.EditorTools
{
    [CustomPropertyDrawer(typeof(UITextStyleDefinition))]
    sealed class UITextStyleDefinitionDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var height = EditorGUIUtility.singleLineHeight;
            if (!property.isExpanded)
            {
                return height;
            }

            var child = property.Copy();
            var end = child.GetEndProperty();
            if (!child.NextVisible(true))
            {
                return height + EditorGUIUtility.standardVerticalSpacing + UITextStylePreview.Height(property);
            }

            while (!SerializedProperty.EqualContents(child, end))
            {
                height += EditorGUIUtility.standardVerticalSpacing + EditorGUI.GetPropertyHeight(child, true);
                if (!child.NextVisible(false))
                {
                    break;
                }
            }

            return height + EditorGUIUtility.standardVerticalSpacing + UITextStylePreview.Height(property);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            var foldout = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            var id = property.FindPropertyRelative("id").stringValue;
            var title = string.IsNullOrEmpty(id) ? label.text : id;
            property.isExpanded = EditorGUI.Foldout(foldout, property.isExpanded, title, true);
            if (!property.isExpanded)
            {
                EditorGUI.EndProperty();
                return;
            }

            EditorGUI.indentLevel++;
            var y = foldout.yMax + EditorGUIUtility.standardVerticalSpacing;
            var child = property.Copy();
            var end = child.GetEndProperty();
            if (child.NextVisible(true))
            {
                while (!SerializedProperty.EqualContents(child, end))
                {
                    var childHeight = EditorGUI.GetPropertyHeight(child, true);
                    EditorGUI.PropertyField(new Rect(position.x, y, position.width, childHeight), child, true);
                    y += childHeight + EditorGUIUtility.standardVerticalSpacing;
                    if (!child.NextVisible(false))
                    {
                        break;
                    }
                }
            }

            var preview = EditorGUI.IndentedRect(new Rect(position.x, y, position.width, UITextStylePreview.Height(property)));
            UITextStylePreview.Draw(preview, property);
            EditorGUI.indentLevel--;
            EditorGUI.EndProperty();
        }
    }

    static class UITextStylePreview
    {
        const float MaxPreviewSize = 64f;

        public static float Height(SerializedProperty style)
        {
            return DisplaySize(style) + 12f;
        }

        public static void Draw(Rect rect, SerializedProperty style)
        {
            var color = style.FindPropertyRelative("color").colorValue;
            var fontAsset = style.FindPropertyRelative("font").objectReferenceValue as TMP_FontAsset;
            var source = fontAsset == null ? null : fontAsset.sourceFontFile;
            var sample = style.FindPropertyRelative("id").stringValue;
            Draw(rect, source, DisplaySize(style), color, string.IsNullOrEmpty(sample) ? "Ag" : sample);
        }

        public static void Draw(Rect rect, Font font, float fontSize, Color color, string sample)
        {
            var luminance = (color.r * 0.299f) + (color.g * 0.587f) + (color.b * 0.114f);
            var background = luminance > 0.7f
                ? new Color(0.16f, 0.17f, 0.2f, 1f)
                : new Color(0.93f, 0.94f, 0.96f, 1f);
            EditorGUI.DrawRect(rect, background);

            if (font == null)
            {
                EditorGUI.LabelField(rect, "Assign a font to preview this style.");
                return;
            }

            var displaySize = Mathf.RoundToInt(fontSize);
            font.RequestCharactersInTexture(sample, displaySize, FontStyle.Normal);
            var preview = new GUIStyle(EditorStyles.label)
            {
                font = font,
                fontSize = displaySize,
                alignment = TextAnchor.MiddleLeft,
                clipping = TextClipping.Clip,
                padding = new RectOffset(8, 8, 0, 0),
            };
            preview.normal.textColor = color;
            GUI.Label(rect, sample, preview);
        }

        static float DisplaySize(SerializedProperty style)
        {
            var size = style.FindPropertyRelative("fontSize").floatValue;
            if (size < 12f)
            {
                return 12f;
            }

            return Mathf.Min(size, MaxPreviewSize);
        }
    }
}
