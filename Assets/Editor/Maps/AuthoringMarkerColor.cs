using UnityEditor;
using UnityEngine;

namespace BattleRoyale.Maps.EditorTools
{
    public static class AuthoringMarkerColor
    {
        public const string ImageUpperLeftKey = "BattleRoyale.MarkerColor.ImageUpperLeft";
        public const string ImageLowerRightKey = "BattleRoyale.MarkerColor.ImageLowerRight";
        public const string TreeUpperLeftKey = "BattleRoyale.MarkerColor.TreeUpperLeft";
        public const string TreeLowerRightKey = "BattleRoyale.MarkerColor.TreeLowerRight";

        static readonly Color ImageUpperLeftDefault = new Color(1f, 0.85f, 0.2f, 1f);
        static readonly Color ImageLowerRightDefault = new Color(0.3f, 0.75f, 1f, 1f);
        static readonly Color TreeUpperLeftDefault = new Color(0.4f, 0.9f, 0.35f, 1f);
        static readonly Color TreeLowerRightDefault = new Color(0.95f, 0.45f, 0.2f, 1f);
        static GUIStyle labelStyle;

        public static Color ImageUpperLeft => Get(ImageUpperLeftKey, ImageUpperLeftDefault);

        public static Color ImageLowerRight => Get(ImageLowerRightKey, ImageLowerRightDefault);

        public static Color TreeUpperLeft => Get(TreeUpperLeftKey, TreeUpperLeftDefault);

        public static Color TreeLowerRight => Get(TreeLowerRightKey, TreeLowerRightDefault);

        public static string Key(MapModePointType type)
        {
            return "BattleRoyale.MarkerColor." + type;
        }

        public static Color For(MapModePointType type)
        {
            var fallback = MapModePointMarker.GetColor(type);
            fallback.a = 1f;
            return Get(Key(type), fallback);
        }

        public static Color Get(string key, Color fallback)
        {
            fallback.a = 1f;
            var html = EditorPrefs.GetString(key, string.Empty);
            if (!ColorUtility.TryParseHtmlString(html, out var color))
            {
                return fallback;
            }

            color.a = 1f;
            return color;
        }

        public static void Set(string key, Color color)
        {
            color.a = 1f;
            EditorPrefs.SetString(key, "#" + ColorUtility.ToHtmlStringRGB(color));
        }

        public static bool StoreIfChanged(string key, Color previous, Color picked)
        {
            previous.a = 1f;
            picked.a = 1f;
            if (Close(previous, picked))
            {
                return false;
            }

            Set(key, picked);
            return true;
        }

        public static void Apply(MeshRenderer renderer, Color color)
        {
            if (renderer == null)
            {
                return;
            }

            color.a = 1f;
            var block = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(block);
            if (Close(block.GetColor("_BaseColor"), color))
            {
                return;
            }

            block.SetColor("_BaseColor", color);
            block.SetColor("_Color", color);
            renderer.SetPropertyBlock(block);
        }

        public static void DrawLabel(Vector3 worldPosition, string text, Color color)
        {
            if (Event.current.type != EventType.Repaint)
            {
                return;
            }

            labelStyle ??= new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 13,
                alignment = TextAnchor.MiddleLeft
            };
            var content = new GUIContent(text);
            var size = labelStyle.CalcSize(content);
            var gui = HandleUtility.WorldToGUIPoint(worldPosition);
            var rect = new Rect(gui.x + 4f, gui.y - size.y * 0.5f, size.x, size.y);
            Handles.BeginGUI();
            labelStyle.normal.textColor = new Color(0f, 0f, 0f, 0.9f);
            GUI.Label(new Rect(rect.x + 1f, rect.y + 1f, rect.width, rect.height), content, labelStyle);
            color.a = 1f;
            labelStyle.normal.textColor = color;
            GUI.Label(rect, content, labelStyle);
            Handles.EndGUI();
        }

        static bool Close(Color a, Color b)
        {
            return Mathf.Abs(a.r - b.r) < 0.001f &&
                   Mathf.Abs(a.g - b.g) < 0.001f &&
                   Mathf.Abs(a.b - b.b) < 0.001f &&
                   Mathf.Abs(a.a - b.a) < 0.001f;
        }
    }
}
