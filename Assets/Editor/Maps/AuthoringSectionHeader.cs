using UnityEditor;
using UnityEngine;

namespace BattleRoyale.Maps.EditorTools
{
    static class AuthoringSectionHeader
    {
        const float Space = 8f;

        static string scope = string.Empty;
        static bool hasSection;
        static bool groupOpen;

        public static void Begin(string scopeName)
        {
            End();
            hasSection = false;
            scope = scopeName ?? string.Empty;
        }

        public static void DrawToolbar(string scopeName)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Expand All", EditorStyles.miniButton, GUILayout.Width(88f)))
                {
                    SetAllOpen(scopeName, true);
                }

                if (GUILayout.Button("Collapse All", EditorStyles.miniButton, GUILayout.Width(96f)))
                {
                    SetAllOpen(scopeName, false);
                }

                GUILayout.Space(4f);
            }
        }

        public static void SetAllOpen(string scopeName, bool open)
        {
            var name = scopeName ?? string.Empty;
            var generation = SessionState.GetInt(ForceGenerationKey(name), 0) + 1;
            SessionState.SetInt(ForceGenerationKey(name), generation);
            SessionState.SetBool(ForceOpenKey(name), open);
        }

        public static bool Draw(string title)
        {
            EndGroup();
            if (hasSection)
            {
                EditorGUILayout.Space(Space);
                var line = GUILayoutUtility.GetRect(0f, 1f, GUILayout.ExpandWidth(true));
                EditorGUI.DrawRect(line, DividerColor);
                EditorGUILayout.Space(Space);
            }

            hasSection = true;
            var key = SectionKey(title);
            var open = SectionIsOpen(key);
            open = EditorGUILayout.BeginFoldoutHeaderGroup(open, title);
            SessionState.SetBool(key, open);
            groupOpen = true;
            if (open)
            {
                EditorGUILayout.Space(Space);
            }

            return open;
        }

        public static void End()
        {
            EndGroup();
            hasSection = false;
        }

        static void EndGroup()
        {
            if (!groupOpen)
            {
                return;
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
            groupOpen = false;
        }

        static bool SectionIsOpen(string key)
        {
            var generation = SessionState.GetInt(ForceGenerationKey(scope), 0);
            var appliedGeneration = SessionState.GetInt(key + ".gen", 0);
            if (generation == appliedGeneration)
            {
                return SessionState.GetBool(key, true);
            }

            var open = SessionState.GetBool(ForceOpenKey(scope), true);
            SessionState.SetBool(key, open);
            SessionState.SetInt(key + ".gen", generation);
            return open;
        }

        static string SectionKey(string title)
        {
            return $"BattleRoyale.MapAuthoring.{scope}.{title}";
        }

        static string ForceGenerationKey(string scopeName)
        {
            return $"BattleRoyale.MapAuthoring.{scopeName}.__generation";
        }

        static string ForceOpenKey(string scopeName)
        {
            return $"BattleRoyale.MapAuthoring.{scopeName}.__open";
        }

        static Color DividerColor =>
            EditorGUIUtility.isProSkin
                ? new Color(0.12f, 0.12f, 0.12f, 1f)
                : new Color(0.6f, 0.6f, 0.6f, 1f);
    }
}
