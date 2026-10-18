#if UNITY_EDITOR
using BattleRoyale.Movement;
using UnityEditor;
using UnityEngine;

namespace BattleRoyale.Maps.EditorTools
{
    internal static class MapGeometrySection
    {
        internal struct Values
        {
            public float WorldScale;
            public float BaseHeight;
            public float WallHeight;
            public float WallThickness;
            public float UnitScale;
            public float ColumnSize;
        }

        public static bool Draw(
            ref Values values,
            bool includeColumnSize,
            out bool changed,
            out bool applyRequested)
        {
            if (!AuthoringSectionHeader.Draw("Geometry"))
            {
                changed = false;
                applyRequested = false;
                return false;
            }

            EditorGUI.BeginChangeCheck();
            values.WorldScale = Mathf.Max(
                0.0001f,
                EditorGUILayout.FloatField(
                    new GUIContent(
                        "World Scale",
                        "One generator unit becomes this many Unity world units."),
                    values.WorldScale));
            values.BaseHeight = EditorGUILayout.FloatField("Base Height", values.BaseHeight);
            values.WallHeight = Mathf.Max(
                0f,
                EditorGUILayout.FloatField("Wall Height", values.WallHeight));
            values.WallThickness = Mathf.Max(
                0.001f,
                EditorGUILayout.FloatField("Wall Thickness", values.WallThickness));
            if (includeColumnSize)
            {
                values.ColumnSize = Mathf.Max(
                    0.01f,
                    EditorGUILayout.FloatField("Column Size", values.ColumnSize));
            }

            values.UnitScale = Mathf.Max(
                0.01f,
                EditorGUILayout.FloatField(
                    new GUIContent(
                        "Unit Scale",
                        "Local scale applied to every UnitMovement object in the loaded scene."),
                    values.UnitScale));
            changed = EditorGUI.EndChangeCheck();
            applyRequested = GUILayout.Button("Apply Unit Scale To Loaded Scene");
            return true;
        }

        public static int ApplyToLoadedUnits(float unitScale)
        {
            var scaledUnitCount = 0;
            foreach (var unit in Resources.FindObjectsOfTypeAll<UnitMovement>())
            {
                if (!unit.gameObject.scene.IsValid() ||
                    !unit.gameObject.scene.isLoaded)
                {
                    continue;
                }

                Undo.RecordObject(unit.transform, "Set Unit Scale");
                unit.transform.localScale = Vector3.one * unitScale;
                EditorUtility.SetDirty(unit.transform);
                scaledUnitCount++;
            }

            return scaledUnitCount;
        }
    }
}
#endif
